using ApexPost.Domain;

namespace ApexPost.Application;

public sealed class ReportService(
    IRepository<Branch> branches, IRepository<Staff> staff, IRepository<Product> products,
    IRepository<StockItem> stock, IRepository<CounterTransaction> transactions,
    IRepository<Shipment> shipments, IRepository<MoneyOrder> moneyOrders,
    IRepository<IdentityVerification> verifications, LowStockSpecification lowStockSpecification) : IReportService
{
    public async Task<DashboardSummary> GetDashboardAsync(string? branchCode, CancellationToken cancellationToken = default)
    {
        var salesTask = transactions.GetAllAsync(cancellationToken);
        var stockTask = stock.GetAllAsync(cancellationToken);
        var staffTask = staff.GetAllAsync(cancellationToken);
        var productTask = products.GetAllAsync(cancellationToken);
        var branchTask = branches.GetAllAsync(cancellationToken);
        await Task.WhenAll(salesTask, stockTask, staffTask, productTask, branchTask);
        var sales = await salesTask; var stockItems = await stockTask; var staffItems = await staffTask; var productItems = await productTask; var branchItems = await branchTask;
        var tx = sales.Where(x => x.Status == TransactionStatus.Paid && (branchCode == null || x.BranchCode == branchCode)).ToArray();
        var today = DateTimeOffset.UtcNow.Date;
        return new DashboardSummary(branchItems.Count, staffItems.Count(x => x.Status == StaffStatus.Active), productItems.Count, stockItems.Count(x => lowStockSpecification.IsSatisfiedBy(x) && (branchCode == null || x.BranchCode == branchCode)), tx.Where(x => x.CreatedAt.UtcDateTime.Date == today).Sum(x => x.Total), tx.Count(x => x.CreatedAt.UtcDateTime.Date == today));
    }

    public async Task<IReadOnlyList<DailySalesRow>> GetDailySalesAsync(DateOnly day, string? branchCode, CancellationToken cancellationToken = default)
    {
        var all = await transactions.GetAllAsync(cancellationToken);
        var selected = all.Where(x => x.Status == TransactionStatus.Paid && DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) == day && (branchCode == null || x.BranchCode == branchCode));
        return selected.GroupBy(x => x.BranchCode).Select(g => new DailySalesRow(g.Key, g.SelectMany(x => x.Lines).Where(l => l.Kind == LineKind.RetailProduct).Sum(l => l.LineTotal), g.SelectMany(x => x.Lines).Where(l => l.Kind != LineKind.RetailProduct).Sum(l => l.LineTotal), g.Sum(x => x.Total), g.Count())).ToArray();
    }

    public async Task<IReadOnlyList<LowStockRow>> GetLowStockAsync(string? branchCode, CancellationToken cancellationToken = default)
    {
        var stocks = await stock.GetAllAsync(cancellationToken); var catalog = await products.GetAllAsync(cancellationToken);
        return (from s in stocks where lowStockSpecification.IsSatisfiedBy(s) && (branchCode == null || s.BranchCode == branchCode) join p in catalog on s.Sku equals p.Sku select new LowStockRow(s.BranchCode, s.Sku, p.Name, s.Quantity)).ToArray();
    }

    public async Task<IReadOnlyList<ShippingVolumeRow>> GetShippingVolumeAsync(string? branchCode, CancellationToken cancellationToken = default)
    {
        var all = await shipments.GetAllAsync(cancellationToken);
        return all.Where(x => branchCode is null || x.BranchCode == branchCode).GroupBy(x => new { x.DestinationZone, x.ServiceLevel }).Select(g => new ShippingVolumeRow(g.Key.DestinationZone, g.Key.ServiceLevel.ToString(), g.Count())).ToArray();
    }

    public async Task<IReadOnlyList<MoneyOrderAuditRow>> GetMoneyOrderAuditAsync(CancellationToken cancellationToken = default)
    {
        var ordersTask = moneyOrders.GetAllAsync(cancellationToken); var checksTask = verifications.GetAllAsync(cancellationToken);
        await Task.WhenAll(ordersTask, checksTask);
        var orders = await ordersTask; var checks = await checksTask;
        return orders.Select(x => x.BranchCode).Union(checks.Select(x => x.BranchCode), StringComparer.OrdinalIgnoreCase).Distinct().Where(x => !string.IsNullOrEmpty(x)).Select(code => new MoneyOrderAuditRow(code, orders.Count(x => x.BranchCode == code), checks.Count(x => x.BranchCode == code && x.Result == VerificationResult.Successful), checks.Count(x => x.BranchCode == code && x.Result == VerificationResult.Failed))).ToArray();
    }
}
