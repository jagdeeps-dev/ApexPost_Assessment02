using ApexPost.Application;
using ApexPost.Domain;
using ApexPost.Web.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexPost.Web.Controllers;

[Authorize]
public sealed class ShippingController(ICourierService courier, ICounterService counter, IShippingPricingStrategy pricingStrategy) : Controller
{
    [HttpGet] public IActionResult Preview() => View(new ShippingViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToTransaction(ShippingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.TransactionId)) return BadRequest(new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(ModelState));
        var staffId = User.FindFirst("StaffId")?.Value;
        if (string.IsNullOrWhiteSpace(staffId)) return Forbid();
        try
        {
            var shipment = new Shipment { Kind = model.Kind, WeightKg = model.WeightKg, LengthCm = model.LengthCm, WidthCm = model.WidthCm, HeightCm = model.HeightCm, DestinationZone = model.DestinationZone, ServiceLevel = model.ServiceLevel };
            await counter.AddShippingLineAsync(model.TransactionId, staffId, shipment, cancellationToken);
            return RedirectToAction("Details", "Transactions", new { id = model.TransactionId });
        }
        catch (DomainException ex) { ModelState.AddModelError(string.Empty, ex.Message); return BadRequest(new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(ModelState)); }
    }

    [HttpGet]
    public IActionResult PreviewResult()
    {
        if (TempData["ShippingEstimate"] is not string json) return RedirectToAction(nameof(Preview));
        var estimate = JsonSerializer.Deserialize<ShippingEstimateViewModel>(json);
        return estimate is null ? RedirectToAction(nameof(Preview)) : View(estimate);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(ShippingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            var rates = await courier.GetRatesAsync(timeout.Token);
            var price = pricingStrategy.Calculate(model.Kind, new Weight(model.WeightKg), model.DestinationZone, model.ServiceLevel, rates);
            TempData["ShippingEstimate"] = JsonSerializer.Serialize(new ShippingEstimateViewModel { Kind = model.Kind, WeightKg = model.WeightKg, LengthCm = model.LengthCm, WidthCm = model.WidthCm, HeightCm = model.HeightCm, DestinationZone = model.DestinationZone, ServiceLevel = model.ServiceLevel, Price = price });
            return RedirectToAction(nameof(PreviewResult));
        }
        catch (DomainException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return StatusCode(StatusCodes.Status504GatewayTimeout, "Shipping rate lookup timed out."); }
    }
}
