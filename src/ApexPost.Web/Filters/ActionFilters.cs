using System.Diagnostics;
using ApexPost.Application;
using ApexPost.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApexPost.Web.Filters;

public sealed class AuditActionFilter(IAuditStore auditStore, ILogger<AuditActionFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew(); var succeeded = false; var statusCode = StatusCodes.Status200OK;
        try
        {
            var executed = await next();
            statusCode = executed.Exception is not null && !executed.ExceptionHandled ? StatusCodes.Status500InternalServerError : executed.Result switch
            {
                ObjectResult obj when obj.StatusCode.HasValue => obj.StatusCode.Value,
                NotFoundResult => StatusCodes.Status404NotFound,
                ForbidResult => StatusCodes.Status403Forbidden,
                UnauthorizedResult => StatusCodes.Status401Unauthorized,
                BadRequestResult => StatusCodes.Status400BadRequest,
                RedirectResult => StatusCodes.Status302Found,
                RedirectToActionResult => StatusCodes.Status302Found,
                RedirectToRouteResult => StatusCodes.Status302Found,
                StatusCodeResult status => status.StatusCode,
                _ => StatusCodes.Status200OK
            };
            succeeded = (executed.Exception is null || executed.ExceptionHandled) && statusCode < StatusCodes.Status400BadRequest && context.ModelState.IsValid;
        }
        catch { succeeded = false; statusCode = StatusCodes.Status500InternalServerError; throw; }
        finally
        {
            stopwatch.Stop();
            var user = context.HttpContext.User;
            var safeValues = context.RouteData.Values.Where(x => x.Key is "id" or "branchCode" or "transactionId").Select(x => $"{x.Key}={Convert.ToString(x.Value)}").ToList();
            foreach (var argument in context.ActionArguments.Where(x => x.Key is "id" or "transactionId"))
            {
                if (argument.Value is string value && value.Length <= 128)
                {
                    safeValues.Add($"{argument.Key}={value}");
                }
            }
            foreach (var argument in context.ActionArguments.Values.Where(x => x is not null))
            {
                var transactionIdProperty = argument!.GetType().GetProperty("TransactionId");
                if (transactionIdProperty?.GetValue(argument) is string transactionId && transactionId.Length <= 128)
                {
                    safeValues.Add($"transactionId={transactionId}");
                }
            }
            foreach (var argument in context.ActionArguments.Values.Where(x => x is not null))
            {
                var verifiedProperty = argument!.GetType().GetProperty("IdentityVerified");
                if (succeeded && verifiedProperty?.GetValue(argument) is true)
                {
                    safeValues.Add("identityVerification=performed");
                }
            }
            var routeValues = string.Join(";", safeValues.Distinct(StringComparer.Ordinal));
            var actionName = context.ActionDescriptor.RouteValues.TryGetValue("action", out var configuredAction)
                ? configuredAction ?? "unknown"
                : "unknown";
            var record = new AuditRecord(Guid.NewGuid().ToString("N"), user.FindFirst("StaffId")?.Value ?? "anonymous", user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "anonymous", user.FindFirst("BranchCode")?.Value ?? "unknown", context.Controller.GetType().Name, actionName, routeValues, statusCode, stopwatch.ElapsedMilliseconds, context.HttpContext.TraceIdentifier, DateTimeOffset.UtcNow, succeeded ? "Succeeded" : "Failed");
            try { await auditStore.AppendAsync(record, context.HttpContext.RequestAborted); }
            catch (Exception ex) { logger.LogCritical(ex, "Audit write failed for action {Action}; correlation {CorrelationId}", record.Action, record.CorrelationId); }
        }
    }
}

public sealed class ValidationActionFilter : IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState));
            return Task.CompletedTask;
        }
        return next();
    }
}

public sealed class PerformanceActionFilter(IConfiguration configuration, ILogger<PerformanceActionFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var watch = Stopwatch.StartNew();
        await next();
        watch.Stop();
        var threshold = configuration.GetValue("Performance:ActionWarningThresholdMs", 500);
        if (watch.ElapsedMilliseconds > threshold) logger.LogWarning("Slow MVC action {Action} took {ElapsedMs}ms; threshold={ThresholdMs}ms; correlation={CorrelationId}", context.ActionDescriptor.DisplayName, watch.ElapsedMilliseconds, threshold, context.HttpContext.TraceIdentifier);
    }
}
