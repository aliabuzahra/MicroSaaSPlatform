using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Service.Features.Usage;
using SaaS.Billing.Service.Infrastructure.Persistence;

namespace SaaS.Billing.Service.Features.Usage;

public static class UsageEndpoints
{
    public static void MapUsageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/billing/usage");

        // Report Usage
        group.MapPost("/events", async (ReportUsageRequest req, UsageService service) =>
        {
            await service.TrackUsageAsync(req.TenantId, req.MetricKey, req.Quantity);
            return Results.Accepted();
        });

        // Get Usage Summary
        group.MapGet("/", async (Guid tenantId, string metricKey, UsageService service) =>
        {
            // Default to last 30 days for now
            var since = DateTime.UtcNow.AddDays(-30);
            var usage = await service.GetUsageAsync(tenantId, metricKey, since);
            var limitVal = await service.GetLimitAsync(tenantId, metricKey);
            bool isAllocated = limitVal.HasValue ? usage < limitVal.Value : true;

            return Results.Ok(new { Usage = usage, Limit = limitVal, IsAllocated = isAllocated });
        });
        // Set Plan Limit (Dev/Admin)
        group.MapPost("/limits", async (SetLimitRequest req, BillingDbContext db) =>
        {
            var limit = await db.PlanLimits.FirstOrDefaultAsync(l => l.PlanId == req.PlanId && l.MetricKey == req.MetricKey);
            if (limit == null)
            {
                limit = new Domain.Entities.PlanLimit(req.PlanId, req.MetricKey, req.MaxQuantity);
                db.PlanLimits.Add(limit);
            }
            else
            {
                // In a real app, we'd update it. For now, just ignore or overwrite.
                // limits are immutable in this simple design, so let's just leave it or strictly add new ones.
                return Results.Conflict("Limit already exists.");
            }
            await db.SaveChangesAsync();
            return Results.Created($"/api/billing/usage/limits/{limit.Id}", limit);
        });
    }

    public record ReportUsageRequest(Guid TenantId, string MetricKey, decimal Quantity);
    public record SetLimitRequest(string PlanId, string MetricKey, decimal MaxQuantity);
}
