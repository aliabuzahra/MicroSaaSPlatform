using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Service.Infrastructure.Persistence;

namespace SaaS.Billing.Service.Features.Billing;

public static class GetSubscriptionEndpoint
{
    public static void MapGetSubscriptionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/billing/subscription/{tenantId}", async (Guid tenantId, BillingDbContext db) =>
        {
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
