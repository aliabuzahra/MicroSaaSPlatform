using Microsoft.EntityFrameworkCore;
using SaaS.Webhooks.Service.Domain.Entities;
using SaaS.Webhooks.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Authorization;

namespace SaaS.Webhooks.Service.Features.Webhooks;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var subscriptionsGroup = app.MapGroup("/api/webhooks/subscriptions").RequireAuthorization();
        var deliveriesGroup = app.MapGroup("/api/webhooks/deliveries").RequireAuthorization();

        subscriptionsGroup.MapPost("/", async (
            CreateSubscriptionRequest request,
            HttpContext context,
            WebhooksDbContext db) =>
        {
            var tenantId = context.GetCurrentTenantId();
            var userId = context.GetCurrentUserId();

            if (!tenantId.HasValue || !userId.HasValue)
                return Results.Unauthorized();

            var invalidEvents = request.Events.Where(e => !WebhookEventTypes.All.Contains(e)).ToList();
            if (invalidEvents.Any())
                return Results.BadRequest(new { Error = $"Invalid events: {string.Join(", ", invalidEvents)}" });

            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || 
                (uri.Scheme != "https" && uri.Scheme != "http"))
                return Results.BadRequest(new { Error = "Invalid URL. Must be a valid HTTP(S) URL." });

            var subscription = WebhookSubscription.Create(
                request.Name,
                request.Url,
                tenantId.Value,
                userId.Value,
                request.Events);

            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync();

            return Results.Created($"/api/webhooks/subscriptions/{subscription.Id}", 
                new SubscriptionResponse(subscription, true));
        });

        subscriptionsGroup.MapGet("/", async (HttpContext context, WebhooksDbContext db) =>
        {
            var subscriptions = await db.Subscriptions
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Results.Ok(subscriptions.Select(s => new SubscriptionResponse(s, false)));
        });

        subscriptionsGroup.MapGet("/{id:guid}", async (Guid id, WebhooksDbContext db) =>
        {
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
            if (subscription is null)
                return Results.NotFound(new { Error = "Subscription not found." });

            return Results.Ok(new SubscriptionResponse(subscription, false));
        });

        subscriptionsGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateSubscriptionRequest request,
            WebhooksDbContext db) =>
        {
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
            if (subscription is null)
                return Results.NotFound(new { Error = "Subscription not found." });

            if (request.Events != null)
            {
                var invalidEvents = request.Events.Where(e => !WebhookEventTypes.All.Contains(e)).ToList();
                if (invalidEvents.Any())
                    return Results.BadRequest(new { Error = $"Invalid events: {string.Join(", ", invalidEvents)}" });
            }

            subscription.Update(request.Name, request.Url, request.Events);
            await db.SaveChangesAsync();

            return Results.Ok(new SubscriptionResponse(subscription, false));
        });

        subscriptionsGroup.MapDelete("/{id:guid}", async (Guid id, WebhooksDbContext db) =>
        {
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
            if (subscription is null)
                return Results.NotFound(new { Error = "Subscription not found." });

            db.Subscriptions.Remove(subscription);
            await db.SaveChangesAsync();

            return Results.Ok(new { Message = "Subscription deleted." });
        });

        subscriptionsGroup.MapPost("/{id:guid}/activate", async (Guid id, WebhooksDbContext db) =>
        {
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
            if (subscription is null)
                return Results.NotFound(new { Error = "Subscription not found." });

            subscription.Activate();
            await db.SaveChangesAsync();

            return Results.Ok(new SubscriptionResponse(subscription, false));
        });

        subscriptionsGroup.MapPost("/{id:guid}/deactivate", async (Guid id, WebhooksDbContext db) =>
        {
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
            if (subscription is null)
                return Results.NotFound(new { Error = "Subscription not found." });

            subscription.Deactivate();
            await db.SaveChangesAsync();

            return Results.Ok(new SubscriptionResponse(subscription, false));
        });

        subscriptionsGroup.MapPost("/{id:guid}/rotate-secret", async (Guid id, WebhooksDbContext db) =>
        {
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id);
            if (subscription is null)
                return Results.NotFound(new { Error = "Subscription not found." });

            var newSecret = subscription.RegenerateSecret();
            await db.SaveChangesAsync();

            return Results.Ok(new { Secret = newSecret });
        });

        deliveriesGroup.MapGet("/", async (
            HttpContext context,
            WebhooksDbContext db,
            Guid? subscriptionId,
            string? status,
            int? page,
            int? pageSize) =>
        {
            var tenantId = context.GetCurrentTenantId();
            if (!tenantId.HasValue)
                return Results.Unauthorized();

            var query = db.Deliveries
                .Where(d => d.TenantId == tenantId.Value);

            if (subscriptionId.HasValue)
                query = query.Where(d => d.SubscriptionId == subscriptionId.Value);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DeliveryStatus>(status, true, out var statusEnum))
                query = query.Where(d => d.Status == statusEnum);

            var totalCount = await query.CountAsync();
            var actualPage = page ?? 1;
            var actualPageSize = Math.Min(pageSize ?? 20, 100);

            var deliveries = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((actualPage - 1) * actualPageSize)
                .Take(actualPageSize)
                .Select(d => new DeliveryResponse(d))
                .ToListAsync();

            return Results.Ok(new { Items = deliveries, TotalCount = totalCount, Page = actualPage, PageSize = actualPageSize });
        });

        deliveriesGroup.MapGet("/{id:guid}", async (Guid id, HttpContext context, WebhooksDbContext db) =>
        {
            var tenantId = context.GetCurrentTenantId();
            if (!tenantId.HasValue)
                return Results.Unauthorized();

            var delivery = await db.Deliveries
                .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value);

            if (delivery is null)
                return Results.NotFound(new { Error = "Delivery not found." });

            return Results.Ok(new DeliveryResponse(delivery));
        });

        app.MapGet("/api/webhooks/event-types", () => Results.Ok(WebhookEventTypes.All));
    }

    public record CreateSubscriptionRequest(string Name, string Url, List<string> Events);
    public record UpdateSubscriptionRequest(string? Name, string? Url, List<string>? Events);

    public record SubscriptionResponse(
        Guid Id,
        string Name,
        string Url,
        string? Secret,
        List<string> Events,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt)
    {
        public SubscriptionResponse(WebhookSubscription s, bool includeSecret) : this(
            s.Id, s.Name, s.Url, includeSecret ? s.Secret : null, s.Events, s.IsActive, s.CreatedAt, s.UpdatedAt)
        { }
    }

    public record DeliveryResponse(
        Guid Id,
        Guid SubscriptionId,
        string EventType,
        string Url,
        string Status,
        int AttemptCount,
        int? ResponseStatusCode,
        string? ErrorMessage,
        DateTime CreatedAt,
        DateTime? DeliveredAt)
    {
        public DeliveryResponse(WebhookDelivery d) : this(
            d.Id, d.SubscriptionId, d.EventType, d.Url, d.Status.ToString(), 
            d.AttemptCount, d.ResponseStatusCode, d.ErrorMessage, d.CreatedAt, d.DeliveredAt)
        { }
    }
}
