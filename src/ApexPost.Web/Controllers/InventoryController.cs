using ApexPost.Application;
using ApexPost.Domain;
using ApexPost.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize(Policy = "BranchManagerOnly")]
public sealed class InventoryController(IRepository<StockItem> stock, IRepository<Staff> staff, DomainNotifications notifications, LowStockRule lowStockRule) : Controller
{
    [HttpGet] public IActionResult Adjust() => View(new StockAdjustmentViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockAdjustmentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || model.Delta == 0) { if (model.Delta == 0) ModelState.AddModelError(nameof(model.Delta), "Adjustment must not be zero."); return View(model); }
        var branch = User.FindFirst("BranchCode")?.Value;
        var staffId = User.FindFirst("StaffId")?.Value;
        if (string.IsNullOrWhiteSpace(branch) || string.IsNullOrWhiteSpace(staffId)) return Forbid();
        var actor = await staff.GetByIdAsync(staffId, cancellationToken);
        if (actor is null || actor.Status != StaffStatus.Active || actor.Role == StaffRole.CounterClerk || actor.Role == StaffRole.Auditor) return Forbid();
        if (actor.Role != StaffRole.Administrator && !string.Equals(actor.HomeBranchCode, branch, StringComparison.OrdinalIgnoreCase)) return Forbid();
        await StockConcurrency.Gate.WaitAsync(cancellationToken);
        try
        {
        var item = (await stock.GetAllAsync(cancellationToken)).FirstOrDefault(x => x.BranchCode == branch && x.Sku.Equals(model.Sku, StringComparison.OrdinalIgnoreCase));
        if (item is null) return NotFound("Stock record was not found for this branch.");
        try
        {
            item.Adjust(model.Delta);
            await stock.UpdateAsync(item, cancellationToken);
            if (lowStockRule.IsLow(item)) notifications.RaiseStockLow(item);
            TempData["Notice"] = "Stock adjustment saved.";
            return RedirectToAction("Index", "Products");
        }
        catch (DomainException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
        }
        finally { StockConcurrency.Gate.Release(); }
    }
}
