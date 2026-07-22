using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Email;
using SaaS.Notifications.Service.Infrastructure.Persistence;

namespace SaaS.Notifications.Service.Services;

public partial class NotificationService
{
    private readonly NotificationsDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(NotificationsDbContext db, IEmailSender emailSender, ILogger<NotificationService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendTemplatedEmailAsync(
        string templateKey,
        string recipient,
        Dictionary<string, string> placeholders,
        Guid? tenantId = null)
    {
        var template = await _db.Templates.FirstOrDefaultAsync(t => t.Key == templateKey && t.IsActive);
        
        if (template == null)
        {
            _logger.LogWarning("Template {TemplateKey} not found or inactive", templateKey);
            return;
        }

        var subject = ReplacePlaceholders(template.Subject, placeholders);
        var body = ReplacePlaceholders(template.Body, placeholders);

        await SendEmailAsync(recipient, subject, body, template.IsHtml, tenantId, templateKey);
    }

    public async Task SendEmailAsync(
        string recipient,
        string subject,
        string body,
        bool isHtml = true,
        Guid? tenantId = null,
        string? templateKey = null)
    {
        var log = NotificationLog.Create(recipient, subject, body, tenantId, "Email", templateKey);
        _db.NotificationLogs.Add(log);
        await _db.SaveChangesAsync();

        try
        {
            var success = await _emailSender.SendAsync(recipient, subject, body, isHtml);
            
            if (success)
            {
                log.MarkSent();
            }
            else
            {
                log.MarkFailed("Email sending failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            log.MarkFailed(ex.Message);
        }

        await _db.SaveChangesAsync();
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;
        foreach (var placeholder in placeholders)
        {
            result = result.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
        }
        return result;
    }
}
