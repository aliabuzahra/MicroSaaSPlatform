using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Notifications.Service.Domain.Entities;

public class NotificationTemplate : Entity<Guid>
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string Channel { get; set; }
    public required string Subject { get; set; }
    public required string BodyTemplate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static NotificationTemplate Create(
        string name,
        string type,
        string channel,
        string subject,
        string bodyTemplate)
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Channel = channel,
            Subject = subject,
            BodyTemplate = bodyTemplate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string RenderBody(Dictionary<string, string> variables)
    {
        var result = BodyTemplate;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }

    public string RenderSubject(Dictionary<string, string> variables)
    {
        var result = Subject;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }
}
