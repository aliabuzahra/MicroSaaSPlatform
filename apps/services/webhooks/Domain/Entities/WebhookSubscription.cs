using SaaS.Shared.Kernel.BuildingBlocks;
using System.Security.Cryptography;

namespace SaaS.Webhooks.Service.Domain.Entities;

public class WebhookSubscription : Entity<Guid>, IMustHaveTenant
{
    public required string Name { get; set; }
    public required string Url { get; set; }
    public required string Secret { get; set; }
    public TenantId TenantId { get; set; } = TenantId.Empty;
    public Guid CreatedByUserId { get; set; }
    public List<string> Events { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static WebhookSubscription Create(
        string name,
        string url,
        Guid tenantId,
        Guid createdByUserId,
        List<string> events)
    {
        return new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = url,
            Secret = GenerateSecret(),
            TenantId = new TenantId(tenantId),
            CreatedByUserId = createdByUserId,
            Events = events,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? name, string? url, List<string>? events)
    {
        if (name != null) Name = name;
        if (url != null) Url = url;
        if (events != null) Events = events;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    public string RegenerateSecret()
    {
        Secret = GenerateSecret();
        UpdatedAt = DateTime.UtcNow;
        return Secret;
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"whsec_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }
}

public class WebhookDelivery : Entity<Guid>
{
    public Guid SubscriptionId { get; set; }
    public Guid TenantId { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public required string Url { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public int AttemptCount { get; set; } = 0;
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public static WebhookDelivery Create(
        Guid subscriptionId,
        Guid tenantId,
        string eventType,
        string payload,
        string url)
    {
        return new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            TenantId = tenantId,
            EventType = eventType,
            Payload = payload,
            Url = url,
            Status = DeliveryStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsDelivered(int statusCode, string? responseBody)
    {
        Status = DeliveryStatus.Delivered;
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
        DeliveredAt = DateTime.UtcNow;
        AttemptCount++;
    }

    public void MarkAsFailed(string errorMessage, int? statusCode = null, string? responseBody = null)
    {
        AttemptCount++;
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
        ErrorMessage = errorMessage;

        if (AttemptCount >= 5)
        {
            Status = DeliveryStatus.Failed;
        }
        else
        {
            Status = DeliveryStatus.Retrying;
            NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, AttemptCount));
        }
    }
}

public enum DeliveryStatus
{
    Pending,
    Delivered,
    Retrying,
    Failed
}

public static class WebhookEventTypes
{
    public const string UserCreated = "user.created";
    public const string TenantCreated = "tenant.created";
    public const string TenantUpdated = "tenant.updated";
    public const string TenantMemberAdded = "tenant.member.added";
    public const string TenantMemberRemoved = "tenant.member.removed";
    public const string SubscriptionCreated = "subscription.created";
    public const string SubscriptionUpdated = "subscription.updated";
    public const string SubscriptionCancelled = "subscription.cancelled";
    public const string InvoicePaid = "invoice.paid";
    public const string InvoiceFailed = "invoice.failed";

    public static readonly string[] All = [
        UserCreated, TenantCreated, TenantUpdated, 
        TenantMemberAdded, TenantMemberRemoved,
        SubscriptionCreated, SubscriptionUpdated, SubscriptionCancelled,
        InvoicePaid, InvoiceFailed
    ];
}
