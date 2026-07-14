using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Audit.Service.Domain.Entities;

public class AuditLog : Entity<Guid>
{
    public required string EventType { get; set; }
    public required string Action { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? TenantId { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;

    public static AuditLog Create(
        string eventType,
        string action,
        Guid? userId = null,
        string? userEmail = null,
        Guid? tenantId = null,
        string? resourceType = null,
        string? resourceId = null,
        string? oldValues = null,
        string? newValues = null,
        string? metadata = null,
        AuditSeverity severity = AuditSeverity.Info)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Action = action,
            UserId = userId,
            UserEmail = userEmail,
            TenantId = tenantId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            OldValues = oldValues,
            NewValues = newValues,
            Metadata = metadata,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        };
    }
}

public enum AuditSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

public static class AuditEventTypes
{
    public const string UserRegistered = "user.registered";
    public const string UserLoggedIn = "user.logged_in";
    public const string UserLoggedOut = "user.logged_out";
    public const string UserPasswordChanged = "user.password_changed";
    public const string UserPasswordReset = "user.password_reset";
    public const string UserEmailVerified = "user.email_verified";
    
    public const string TenantCreated = "tenant.created";
    public const string TenantUpdated = "tenant.updated";
    public const string TenantDeactivated = "tenant.deactivated";
    public const string TenantMemberAdded = "tenant.member_added";
    public const string TenantMemberRemoved = "tenant.member_removed";
    public const string TenantInvitationSent = "tenant.invitation_sent";
    
    public const string SubscriptionCreated = "subscription.created";
    public const string SubscriptionUpdated = "subscription.updated";
    public const string SubscriptionCancelled = "subscription.cancelled";
    
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentFailed = "payment.failed";
}

public static class AuditActions
{
    public const string Create = "create";
    public const string Read = "read";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string Login = "login";
    public const string Logout = "logout";
    public const string Send = "send";
    public const string Verify = "verify";
    public const string Reset = "reset";
}

public static class AuditResourceTypes
{
    public const string User = "user";
    public const string Tenant = "tenant";
    public const string TenantMember = "tenant_member";
    public const string TenantInvitation = "tenant_invitation";
    public const string Subscription = "subscription";
    public const string Payment = "payment";
    public const string Notification = "notification";
}
