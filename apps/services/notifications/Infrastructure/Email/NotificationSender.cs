using Microsoft.EntityFrameworkCore;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Persistence;

namespace SaaS.Notifications.Service.Infrastructure.Email;

public interface INotificationSender
{
    Task<Notification> SendNotificationAsync(
        Guid userId,
        Guid? tenantId,
        string notificationType,
        string recipient,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default);
}

public class NotificationSender : INotificationSender
{
    private readonly NotificationsDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(
        NotificationsDbContext db,
        IEmailService emailService,
        ILogger<NotificationSender> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Notification> SendNotificationAsync(
        Guid userId,
        Guid? tenantId,
        string notificationType,
        string recipient,
        Dictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        var template = await _db.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Type == notificationType && 
                                       t.Channel == NotificationChannels.Email && 
                                       t.IsActive, cancellationToken);

        if (template is null)
        {
            _logger.LogWarning("No active template found for notification type: {Type}", notificationType);
            throw new InvalidOperationException($"No active template found for notification type: {notificationType}");
        }

        var subject = template.RenderSubject(variables);
        var body = template.RenderBody(variables);

        var notification = Notification.Create(
            userId,
            tenantId,
            notificationType,
            NotificationChannels.Email,
            recipient,
            subject,
            body);

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        var result = await _emailService.SendEmailAsync(recipient, subject, body, cancellationToken);

        if (result.Success)
        {
            notification.MarkAsSent(result.MessageId);
            _logger.LogInformation("Notification {NotificationId} sent successfully to {Recipient}", 
                notification.Id, recipient);
        }
        else
        {
            notification.MarkAsFailed(result.Error ?? "Unknown error");
            _logger.LogError("Failed to send notification {NotificationId} to {Recipient}: {Error}", 
                notification.Id, recipient, result.Error);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return notification;
    }
}
