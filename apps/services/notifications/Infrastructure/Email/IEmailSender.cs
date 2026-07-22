namespace SaaS.Notifications.Service.Infrastructure.Email;

public interface IEmailSender
{
    Task<bool> SendAsync(string to, string subject, string body, bool isHtml = true);
}

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string to, string subject, string body, bool isHtml = true)
    {
        _logger.LogInformation(
            "EMAIL SENT (Console Simulation)\n" +
            "To: {To}\n" +
            "Subject: {Subject}\n" +
            "Body: {Body}",
            to, subject, body.Length > 200 ? body.Substring(0, 200) + "..." : body
        );
        
        return Task.FromResult(true);
    }
}

public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly IConfiguration _config;

    public SmtpEmailSender(ILogger<SmtpEmailSender> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<bool> SendAsync(string to, string subject, string body, bool isHtml = true)
    {
        var smtpHost = _config["Smtp:Host"];
        var smtpPort = _config.GetValue<int>("Smtp:Port", 587);
        var smtpUser = _config["Smtp:Username"];
        var smtpPass = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:From"] ?? "noreply@microsaas.local";

        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("SMTP not configured, falling back to console logging");
            _logger.LogInformation("EMAIL: To={To}, Subject={Subject}", to, subject);
            return true;
        }

        try
        {
            using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort);
            
            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
            {
                client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;
            }

            var message = new System.Net.Mail.MailMessage(fromEmail, to, subject, body)
            {
                IsBodyHtml = isHtml
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }
}
