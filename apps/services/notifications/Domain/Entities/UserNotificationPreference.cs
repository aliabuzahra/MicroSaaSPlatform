using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Notifications.Service.Domain.Entities;

public class UserNotificationPreference : Entity<Guid>
{
    public required Guid UserId { get; set; }
    public required string NotificationType { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static UserNotificationPreference CreateDefault(Guid userId, string notificationType)
    {
        return new UserNotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NotificationType = notificationType,
            EmailEnabled = true,
            SmsEnabled = false,
            PushEnabled = true,
            InAppEnabled = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsChannelEnabled(string channel) => channel switch
    {
        NotificationChannels.Email => EmailEnabled,
        NotificationChannels.Sms => SmsEnabled,
        NotificationChannels.Push => PushEnabled,
        NotificationChannels.InApp => InAppEnabled,
        _ => false
    };

    public void UpdatePreference(bool? email, bool? sms, bool? push, bool? inApp)
    {
        if (email.HasValue) EmailEnabled = email.Value;
        if (sms.HasValue) SmsEnabled = sms.Value;
        if (push.HasValue) PushEnabled = push.Value;
        if (inApp.HasValue) InAppEnabled = inApp.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}
