using ApexPost.Domain;

namespace ApexPost.Application;

public sealed class CounterService(
    IRepository<CounterTransaction> transactions,
    IRepository<Staff> staffRepository,
    IRepository<Branch> branches,
    IRepository<Product> products,
    IRepository<StockItem> stock,
    ICourierService courier,
    IRepository<Shipment> shipments,
    IRepository<IdentityVerification> verifications,
    IRepository<MoneyOrder> moneyOrders,
    MoneyOrderPolicy moneyOrderPolicy,
    DomainNotifications notifications, IShippingPricingStrategy pricingStrategy, LowStockRule lowStockRule) : ICounterService
{
    

    public async Task<CounterTransaction> CreateAsync(string branchCode, string counterId, string clerkId, CancellationToken cancellationToken = default)
    {
        var staff = await staffRepository.GetByIdAsync(clerkId, cancellationToken) ?? throw new DomainException("Staff member was not found.");
        var branch = (await branches.GetAllAsync(cancellationToken)).FirstOrDefault(x => string.Equals(x.Code, branchCode, StringComparison.OrdinalIgnoreCase)) ?? throw new DomainException("Branch was not found.");
        BusinessRules.EnsureCanTransact(staff, branch, branchCode);
        var invalid = ReflectionValidator.Validate(branch).Concat(ReflectionValidator.Validate(staff)).ToArray();
        if (invalid.Length > 0) throw new DomainException(string.Join(" ", invalid));
        if (!branch.Counters.Any(x => x.Id == counterId && x.IsActive)) throw new DomainException("Counter is unavailable.");
        var transaction = new CounterTransaction { BranchCode = branchCode, CounterId = counterId, ClerkId = clerkId };
        await transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<CounterTransaction> AddRetailLineAsync(string transactionId, string actorStaffId, string sku, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        await StockConcurrency.Gate.WaitAsync(cancellationToken);
        try
        {
            var transaction = await RequiredTransaction(transactionId, cancellationToken);
            await EnsureTransactionCanProcessAsync(transaction, actorStaffId, cancellationToken);
            if (transaction.Status != TransactionStatus.Open) throw new DomainException("Only open transactions can be processed.");
            var normalizedSku = new Sku(sku).Value;
            var product = (await products.GetAllAsync(cancellationToken)).FirstOrDefault(x => x.IsActive && string.Equals(x.Sku, normalizedSku, StringComparison.OrdinalIgnoreCase)) ?? throw new DomainException("Product was not found.");
            var stockItem = (await stock.GetAllAsync(cancellationToken)).FirstOrDefault(x => x.BranchCode == transaction.BranchCode && x.Sku == product.Sku) ?? throw new DomainException("Stock record was not found.");
            if (stockItem.Quantity < quantity) throw new DomainException("Insufficient stock.");
            var taxRate = BusinessRules.TaxRate(product.TaxCategory);
            transaction.AddLine(new TransactionLine(Guid.NewGuid().ToString("N"), LineKind.RetailProduct, product.Name, product.Sku, quantity, product.UnitPrice, taxRate, decimal.Round(product.UnitPrice * quantity * (1 + taxRate), 2, MidpointRounding.AwayFromZero)));
            stockItem.Adjust(-quantity);
            try
            {
                await stock.UpdateAsync(stockItem, cancellationToken);
                await transactions.UpdateAsync(transaction, cancellationToken);
            }
            catch
            {
                // Best-effort compensating write for the single-process JSON store; cross-file atomicity is not claimed.
                stockItem.Adjust(quantity);
                await stock.UpdateAsync(stockItem, CancellationToken.None);
                throw;
            }
            if (lowStockRule.IsLow(stockItem)) notifications.RaiseStockLow(stockItem);
            return transaction;
        }
        finally { StockConcurrency.Gate.Release(); }
    }

    public async Task<CounterTransaction> AddShippingLineAsync(string transactionId, string actorStaffId, Shipment shipment, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredTransaction(transactionId, cancellationToken);
        await EnsureTransactionCanProcessAsync(transaction, actorStaffId, cancellationToken);
        if (transaction.Status != TransactionStatus.Open) throw new DomainException("Only open transactions can be processed.");
        if (shipment.LengthCm <= 0m || shipment.WidthCm <= 0m || shipment.HeightCm <= 0m) throw new DomainException("Shipment dimensions must be greater than zero.");
        var allRates = await courier.GetRatesAsync(cancellationToken);
        shipment.Price = pricingStrategy.Calculate(shipment.Kind, new Weight(shipment.WeightKg), shipment.DestinationZone, shipment.ServiceLevel, allRates);
        shipment.BranchCode = transaction.BranchCode;
        await shipments.AddAsync(shipment, cancellationToken);
        transaction.AddLine(new TransactionLine(Guid.NewGuid().ToString("N"), LineKind.Shipping, $"{shipment.Kind} — {shipment.DestinationZone} / {shipment.ServiceLevel}", null, 1, shipment.Price, 0m, shipment.Price));
        await transactions.UpdateAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<CounterTransaction> AddMoneyOrderLineAsync(string transactionId, string actorStaffId, MoneyOrder order, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredTransaction(transactionId, cancellationToken);
        await EnsureTransactionCanProcessAsync(transaction, actorStaffId, cancellationToken);
        if (transaction.Status != TransactionStatus.Open) throw new DomainException("Only open transactions can be processed.");
        if (!string.Equals(transaction.BranchCode, order.BranchCode, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Money order and counter transaction must belong to the same branch.");
        if (order.Amount <= 0m || order.Fee < 0m) throw new DomainException("Money-order amount and fee are invalid.");
        if (moneyOrderPolicy.RequiresIdentityVerification(order.Amount))
        {
            var verification = string.IsNullOrWhiteSpace(order.IdentityVerificationId) ? null : await verifications.GetByIdAsync(order.IdentityVerificationId, cancellationToken);
            BusinessRules.EnsureMoneyOrderVerified(order.Amount, moneyOrderPolicy.Threshold, verification is not null && verification.Result == VerificationResult.Successful && verification.CounterTransactionId == transaction.Id && verification.BranchCode.Equals(transaction.BranchCode, StringComparison.OrdinalIgnoreCase));
        }
        transaction.AddLine(new TransactionLine(Guid.NewGuid().ToString("N"), LineKind.MoneyOrder, $"Money order to {order.Recipient}", null, 1, order.Amount + order.Fee, 0m, order.Amount + order.Fee));
        await transactions.UpdateAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<CounterTransaction> CompleteAsync(string transactionId, string actorStaffId, PaymentMethod method, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredTransaction(transactionId, cancellationToken);
        await EnsureTransactionCanProcessAsync(transaction, actorStaffId, cancellationToken);
        transaction.MarkPaid(method);
        await transactions.UpdateAsync(transaction, cancellationToken);
        foreach (var order in (await moneyOrders.GetAllAsync(cancellationToken)).Where(x => x.CounterTransactionId == transaction.Id && x.Status == MoneyOrderStatus.PendingPayment))
        {
            order.Status = MoneyOrderStatus.Issued;
            await moneyOrders.UpdateAsync(order, cancellationToken);
            notifications.RaiseMoneyOrderIssued(order);
        }
        notifications.RaiseTransactionCompleted(transaction);
        return transaction;
    }

    public async Task<CounterTransaction> VoidAsync(string transactionId, string staffId, string reason, CancellationToken cancellationToken = default) => await ChangePaidTransactionAsync(transactionId, staffId, reason, false, cancellationToken);
    public async Task<CounterTransaction> RefundAsync(string transactionId, string staffId, string reason, CancellationToken cancellationToken = default) => await ChangePaidTransactionAsync(transactionId, staffId, reason, true, cancellationToken);

    private async Task<CounterTransaction> ChangePaidTransactionAsync(string transactionId, string staffId, string reason, bool refund, CancellationToken cancellationToken)
    {
        var staff = await staffRepository.GetByIdAsync(staffId, cancellationToken) ?? throw new DomainException("Staff member was not found.");
        var transaction = await RequiredTransaction(transactionId, cancellationToken);
        if (staff.Status != StaffStatus.Active) throw new DomainException("Inactive staff cannot process transactions.");
        if (staff.Role != StaffRole.Administrator && !string.Equals(staff.HomeBranchCode, transaction.BranchCode, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Staff can only process transactions for their assigned branch.");
        BusinessRules.EnsureManager(staff, reason);
        if (refund) transaction.Refund(reason); else transaction.Void(reason);
        await transactions.UpdateAsync(transaction, cancellationToken);
        foreach (var order in (await moneyOrders.GetAllAsync(cancellationToken)).Where(x => x.CounterTransactionId == transaction.Id && x.Status != MoneyOrderStatus.Cancelled))
        {
            order.Status = MoneyOrderStatus.Cancelled;
            await moneyOrders.UpdateAsync(order, cancellationToken);
        }
        if (refund) notifications.RaiseTransactionRefunded(transaction); else notifications.RaiseTransactionVoided(transaction);
        return transaction;
    }

    private async Task EnsureTransactionCanProcessAsync(CounterTransaction transaction, string actorStaffId, CancellationToken token)
    {
        var actor = await staffRepository.GetByIdAsync(actorStaffId, token) ?? throw new DomainException("Staff member was not found.");
        var branch = (await branches.GetAllAsync(token)).FirstOrDefault(x => x.Code.Equals(transaction.BranchCode, StringComparison.OrdinalIgnoreCase)) ?? throw new DomainException("Branch was not found.");
        BusinessRules.EnsureCanTransact(actor, branch, transaction.BranchCode);
    }

    private async Task<CounterTransaction> RequiredTransaction(string id, CancellationToken token) => await transactions.GetByIdAsync(id, token) ?? throw new DomainException("Transaction was not found.");
}
