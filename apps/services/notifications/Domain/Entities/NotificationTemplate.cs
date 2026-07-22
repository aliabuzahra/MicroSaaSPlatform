using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Notifications.Service.Domain.Entities;

public class NotificationTemplate : Entity<Guid>
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static NotificationTemplate Create(string key, string name, string subject, string body, bool isHtml = true)
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            Subject = subject,
            Body = body,
            IsHtml = isHtml
        };
    }
}
