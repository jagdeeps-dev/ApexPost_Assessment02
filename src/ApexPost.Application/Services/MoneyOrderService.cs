using ApexPost.Domain;

namespace ApexPost.Application;

public sealed class MoneyOrderService(
    IRepository<MoneyOrder> orders,
    IRepository<IdentityVerification> verifications,
    IRepository<Branch> branches,
    IRepository<Staff> staff,
    IRepository<CounterTransaction> transactions,
    ICounterService counter,
    MoneyOrderPolicy policy) : IMoneyOrderService
{
    public async Task<MoneyOrder> IssueAsync(IssueMoneyOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Amount <= 0m || command.Amount > 1_000_000m)
        {
            throw new DomainException("Money-order amount must be between 0.01 and 1,000,000.00.");
        }
        if (!Enum.IsDefined(typeof(PaymentMethod), command.PaymentMethod))
        {
            throw new DomainException("Unsupported payment method.");
        }
        if (!Enum.IsDefined(typeof(IdentityDocumentType), command.DocumentType))
        {
            throw new DomainException("Unsupported identity document type.");
        }
        if (string.IsNullOrWhiteSpace(command.Sender) || string.IsNullOrWhiteSpace(command.Recipient))
        {
            throw new DomainException("Sender and recipient are required.");
        }
        var employee = await staff.GetByIdAsync(command.StaffId, cancellationToken) ?? throw new DomainException("Staff member was not found.");
        if (employee.Status != StaffStatus.Active)
        {
            throw new DomainException("Inactive staff cannot issue money orders.");
        }

        CounterTransaction? existingTransaction = null;
        if (!string.IsNullOrWhiteSpace(command.ExistingTransactionId))
        {
            existingTransaction = await transactions.GetByIdAsync(command.ExistingTransactionId, cancellationToken) ?? throw new DomainException("Counter transaction was not found.");
            if (existingTransaction.Status != TransactionStatus.Open)
            {
                throw new DomainException("A money order can only be added to an open counter transaction.");
            }
            if (!existingTransaction.BranchCode.Equals(command.BranchCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("Money order and counter transaction must belong to the same branch.");
            }
        }

        var branchCode = existingTransaction?.BranchCode ?? command.BranchCode;
        var branch = (await branches.GetAllAsync(cancellationToken)).FirstOrDefault(x => x.Code.Equals(branchCode, StringComparison.OrdinalIgnoreCase)) ?? throw new DomainException("Branch was not found.");
        BusinessRules.EnsureCanTransact(employee, branch, branchCode);
        if (policy.RequiresIdentityVerification(command.Amount) && (!command.IdentityVerified || string.IsNullOrWhiteSpace(command.DocumentNumber)))
        {
            if (!string.IsNullOrWhiteSpace(command.DocumentNumber))
            {
                await verifications.AddAsync(new IdentityVerification { BranchCode = branchCode, CounterTransactionId = existingTransaction?.Id ?? string.Empty, DocumentType = command.DocumentType, MaskedDocumentNumber = IdentityVerification.Mask(command.DocumentNumber), Result = VerificationResult.Failed, VerifiedByStaffId = command.StaffId }, cancellationToken);
            }
            throw new DomainException($"A successful identity verification is required for money orders above {policy.Threshold:0.00}.");
        }

        CounterTransaction transaction;
        if (existingTransaction is not null)
        {
            transaction = existingTransaction;
        }
        else
        {
            var counterId = branch.Counters.FirstOrDefault(x => x.IsActive)?.Id;
            if (string.IsNullOrWhiteSpace(counterId))
            {
                throw new DomainException("No active counter is configured for this branch.");
            }
            transaction = await counter.CreateAsync(branchCode, counterId, command.StaffId, cancellationToken);
        }

        IdentityVerification? verification = null;
        if (policy.RequiresIdentityVerification(command.Amount))
        {
            verification = new IdentityVerification { BranchCode = branchCode, CounterTransactionId = transaction.Id, DocumentType = command.DocumentType, MaskedDocumentNumber = IdentityVerification.Mask(command.DocumentNumber), Result = VerificationResult.Successful, VerifiedByStaffId = command.StaffId };
            BusinessRules.EnsureMoneyOrderVerified(command.Amount, policy.Threshold, verification.Result == VerificationResult.Successful && verification.CounterTransactionId == transaction.Id);
            await verifications.AddAsync(verification, cancellationToken);
        }
        var fee = decimal.Round(Math.Max(10m, command.Amount * 0.01m), 2, MidpointRounding.AwayFromZero);
        var order = new MoneyOrder { Amount = command.Amount, Sender = command.Sender.Trim(), Recipient = command.Recipient.Trim(), Fee = fee, BranchCode = branchCode, CounterTransactionId = transaction.Id, Status = MoneyOrderStatus.PendingPayment, IdentityVerificationId = verification?.Id };
        await counter.AddMoneyOrderLineAsync(transaction.Id, command.StaffId, order, cancellationToken);
        await orders.AddAsync(order, cancellationToken);
        if (existingTransaction is null)
        {
            await counter.CompleteAsync(transaction.Id, command.StaffId, command.PaymentMethod, cancellationToken);
        }
        return order;
    }
}
