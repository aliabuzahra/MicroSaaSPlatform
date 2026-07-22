using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Notifications.Service.Domain.Entities;

public class NotificationLog : Entity<Guid>
{
    public Guid? TenantId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Channel { get; set; } = "Email";
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public string? TemplateKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    public static NotificationLog Create(
        string recipient,
        string subject,
        string body,
        Guid? tenantId = null,
        string channel = "Email",
        string? templateKey = null)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Channel = channel,
            TemplateKey = templateKey,
            Status = "Pending"
        };
    }

    public void MarkSent()
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        ErrorMessage = error;
    }
}
