using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaaS.Webhooks.Service.Domain.Entities;
using SaaS.Webhooks.Service.Infrastructure.Persistence;

namespace SaaS.Webhooks.Service.Services;

public interface IWebhookDeliveryService
{
    Task DeliverWebhookAsync(Guid tenantId, string eventType, object payload, CancellationToken cancellationToken = default);
    Task RetryFailedDeliveriesAsync(CancellationToken cancellationToken = default);
}

public class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly WebhooksDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    public WebhookDeliveryService(
        WebhooksDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DeliverWebhookAsync(Guid tenantId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _db.Subscriptions
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == new SaaS.Shared.Kernel.BuildingBlocks.TenantId(tenantId) && 
                        s.IsActive && 
                        s.Events.Contains(eventType))
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No active webhook subscriptions found for tenant {TenantId} event {EventType}", 
                tenantId, eventType);
            return;
        }

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        foreach (var subscription in subscriptions)
        {
            var delivery = WebhookDelivery.Create(
                subscription.Id,
                tenantId,
                eventType,
                payloadJson,
                subscription.Url);

            _db.Deliveries.Add(delivery);
            await _db.SaveChangesAsync(cancellationToken);

            await DeliverAsync(delivery, subscription.Secret, cancellationToken);
        }
    }

    public async Task RetryFailedDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        var deliveries = await _db.Deliveries
            .Where(d => d.Status == DeliveryStatus.Retrying && d.NextRetryAt <= DateTime.UtcNow)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var delivery in deliveries)
        {
            var subscription = await _db.Subscriptions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == delivery.SubscriptionId, cancellationToken);

            if (subscription == null || !subscription.IsActive)
            {
                delivery.MarkAsFailed("Subscription not found or inactive");
                await _db.SaveChangesAsync(cancellationToken);
                continue;
            }

            await DeliverAsync(delivery, subscription.Secret, cancellationToken);
        }
    }

    private async Task DeliverAsync(WebhookDelivery delivery, string secret, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("webhook");
            client.Timeout = TimeSpan.FromSeconds(30);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var signature = ComputeSignature(delivery.Payload, secret, timestamp);

            var request = new HttpRequestMessage(HttpMethod.Post, delivery.Url)
            {
                Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Webhook-Id", delivery.Id.ToString());
            request.Headers.Add("X-Webhook-Timestamp", timestamp.ToString());
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", delivery.EventType);

            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                delivery.MarkAsDelivered((int)response.StatusCode, responseBody);
                _logger.LogInformation("Webhook delivered successfully: {DeliveryId} to {Url}", 
                    delivery.Id, delivery.Url);
            }
            else
            {
                delivery.MarkAsFailed($"HTTP {(int)response.StatusCode}", (int)response.StatusCode, responseBody);
                _logger.LogWarning("Webhook delivery failed: {DeliveryId} to {Url} - {StatusCode}", 
                    delivery.Id, delivery.Url, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            delivery.MarkAsFailed(ex.Message);
            _logger.LogError(ex, "Webhook delivery error: {DeliveryId} to {Url}", delivery.Id, delivery.Url);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string ComputeSignature(string payload, string secret, long timestamp)
    {
        var signedPayload = $"{timestamp}.{payload}";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return $"v1={Convert.ToHexString(hash).ToLower()}";
    }
}
