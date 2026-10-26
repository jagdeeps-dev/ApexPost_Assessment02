using ApexPost.Application;
using ApexPost.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class ProductsController(IRepository<Product> products, IRepository<StockItem> stock, LowStockRule lowStockRule) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var allProducts = await products.GetAllAsync(cancellationToken);
        var allStock = await stock.GetAllAsync(cancellationToken);
        var branch = User.FindFirst("BranchCode")?.Value;
        ViewBag.LowStockThreshold = lowStockRule.Threshold;
        var view = allProducts.Select(p => new ProductStockRow(p.Sku, p.Name, p.Category.ToString(), p.UnitPrice, p.TaxCategory.ToString(), allStock.Where(s => s.Sku == p.Sku && (User.IsInRole("Auditor") || User.IsInRole("Administrator") || s.BranchCode == branch)).Sum(s => s.Quantity))).ToArray();
        return View(view);
    }
    public sealed record ProductStockRow(string Sku, string Name, string Category, decimal UnitPrice, string TaxCategory, int Quantity);
}
