namespace SaaS.Notifications.Service.Infrastructure.Email;

public interface IEmailService
{
    Task<EmailSendResult> SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task<EmailSendResult> SendEmailAsync(string to, string subject, string htmlBody, string? textBody, CancellationToken cancellationToken = default);
}

public record EmailSendResult(bool Success, string? MessageId, string? Error);
