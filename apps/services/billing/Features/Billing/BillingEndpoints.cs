using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Service.Domain.Entities;
using SaaS.Billing.Service.Infrastructure.Persistence;

namespace SaaS.Billing.Service.Features.Billing;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/billing");

        group.MapPost("/plans", async (CreatePlanRequest request, BillingDbContext db) =>
        {
            // In real app: Create Product/Price in Stripe here via StripeClient

            var plan = BillingPlan.Create(request.Name, $"price_mock_{Guid.NewGuid()}", request.Amount);
            
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            return Results.Created($"/billing/plans/{plan.Id}", plan);
        });

        group.MapGet("/plans", async (BillingDbContext db) =>
        {
            return Results.Ok(await db.Plans.ToListAsync());
        });

        group.MapPost("/subscribe", async (SubscribeRequest request, BillingDbContext db) =>
        {
            var plan = await db.Plans.FindAsync(request.PlanId);
            if (plan is null) return Results.NotFound("Plan not found.");

            // In real app: Create Subscription in Stripe here via StripeClient

            var subscription = Subscription.Create(
                request.TenantId, 
                plan.StripePriceId, 
                $"sub_mock_{Guid.NewGuid()}", 
                DateTime.UtcNow.AddMonths(1)
            );

            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync();

            return Results.Ok(subscription);
        });
    }

    public record CreatePlanRequest(string Name, decimal Amount);
    public record SubscribeRequest(Guid TenantId, Guid PlanId);
}
