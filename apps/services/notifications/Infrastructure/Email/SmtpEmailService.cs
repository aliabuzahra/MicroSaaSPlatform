using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SaaS.Notifications.Service.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<EmailSendResult> SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        return SendEmailAsync(to, subject, htmlBody, null, cancellationToken);
    }

    public async Task<EmailSendResult> SendEmailAsync(string to, string subject, string htmlBody, string? textBody, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("SMTP is disabled. Email to {To} with subject '{Subject}' would have been sent.", to, subject);
            return new EmailSendResult(true, $"simulated-{Guid.NewGuid()}", null);
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            
            message.To.Add(to);

            if (!string.IsNullOrEmpty(textBody))
            {
                var textView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                message.AlternateViews.Add(textView);
                message.AlternateViews.Add(htmlView);
            }

            await client.SendMailAsync(message, cancellationToken);

            var messageId = Guid.NewGuid().ToString();
            _logger.LogInformation("Email sent successfully to {To}. MessageId: {MessageId}", to, messageId);
            
            return new EmailSendResult(true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Error}", to, ex.Message);
            return new EmailSendResult(false, null, ex.Message);
        }
    }
}

public class SmtpSettings
{
    public const string SectionName = "Smtp";
    
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@saasplatform.com";
    public string FromName { get; set; } = "SaaS Platform";
}
