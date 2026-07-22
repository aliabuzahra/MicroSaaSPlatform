using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Service.Infrastructure.Persistence;

namespace SaaS.Billing.Service.Features.Billing;

public static class GetSubscriptionEndpoint
{
    public static void MapGetSubscriptionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/billing/subscription/{tenantId}", async (Guid tenantId, HttpContext httpContext, BillingDbContext db) =>
        {
            // Verify the requesting user belongs to the requested tenant
            var requestingTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(requestingTenantId) && 
                Guid.TryParse(requestingTenantId, out var reqTenantId) &&
                reqTenantId != tenantId)
            {
                return Results.StatusCode(403);
            }
            
            var subscription = await db.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (subscription == null)
            {
                return Results.NotFound(new { Message = "No subscription found for this tenant." });
            }

            return Results.Ok(new
            {
                subscription.Id,
                subscription.TenantId,
                subscription.Status,
                subscription.PlanId,
                subscription.CurrentPeriodEnd,
                subscription.UpdateUrl,
                subscription.CancelUrl
            });
        })
        .WithTags("Billing")
        .WithOpenApi();
    }
}
