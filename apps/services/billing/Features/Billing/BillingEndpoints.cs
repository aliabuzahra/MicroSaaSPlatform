using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Service.Domain.Entities;
using SaaS.Billing.Service.Infrastructure.Persistence;

namespace SaaS.Billing.Service.Features.Billing;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/billing");

        group.MapPost("/plans", async (CreatePlanRequest request, BillingDbContext db) =>
        {
            var plan = BillingPlan.Create(request.Name, $"price_mock_{Guid.NewGuid()}", request.Amount);
            
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            return Results.Created($"/api/billing/plans/{plan.Id}", plan);
        });

        group.MapGet("/plans", async (BillingDbContext db) =>
        {
            return Results.Ok(await db.Plans.ToListAsync());
        });

        group.MapGet("/plans/{id:guid}", async (Guid id, BillingDbContext db) =>
        {
            var plan = await db.Plans.FindAsync(id);
            if (plan is null) return Results.NotFound(new { Error = "Plan not found." });
            return Results.Ok(plan);
        });

        group.MapPost("/subscribe", async (SubscribeRequest request, BillingDbContext db) =>
        {
            var plan = await db.Plans.FindAsync(request.PlanId);
            if (plan is null) return Results.NotFound(new { Error = "Plan not found." });

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

        group.MapGet("/subscriptions", async (Guid? tenantId, BillingDbContext db) =>
        {
            var query = db.Subscriptions.AsQueryable();
            if (tenantId.HasValue)
            {
                query = query.Where(s => s.TenantId == tenantId.Value);
            }
            return Results.Ok(await query.ToListAsync());
        });
    }

    public record CreatePlanRequest(string Name, decimal Amount);
    public record SubscribeRequest(Guid TenantId, Guid PlanId);
}
