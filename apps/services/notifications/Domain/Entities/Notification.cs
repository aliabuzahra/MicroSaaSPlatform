using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Notifications.Service.Domain.Entities;

public class Notification : Entity<Guid>
{
    public required Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public required string Type { get; set; }
    public required string Channel { get; set; }
    public required string Recipient { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ExternalId { get; set; }

    public static Notification Create(
        Guid userId,
        Guid? tenantId,
        string type,
        string channel,
        string recipient,
        string subject,
        string body)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Type = type,
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsSent(string? externalId = null)
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ExternalId = externalId;
    }

    public void MarkAsFailed(string reason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = reason;
        RetryCount++;
    }

    public void MarkAsRead()
    {
        ReadAt = DateTime.UtcNow;
    }
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Cancelled
}

public static class NotificationTypes
{
    public const string WelcomeEmail = "welcome_email";
    public const string PasswordReset = "password_reset";
    public const string EmailVerification = "email_verification";
    public const string TenantInvitation = "tenant_invitation";
    public const string SubscriptionUpdated = "subscription_updated";
    public const string PaymentFailed = "payment_failed";
    public const string PaymentSuccessful = "payment_successful";
}

public static class NotificationChannels
{
    public const string Email = "email";
    public const string Sms = "sms";
    public const string Push = "push";
    public const string InApp = "in_app";
}
