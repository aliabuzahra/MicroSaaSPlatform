using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Email;

namespace SaaS.Notifications.Service.Consumers;

public class EmailVerificationRequestedConsumer : IConsumer<EmailVerificationRequestedEvent>
{
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<EmailVerificationRequestedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public EmailVerificationRequestedConsumer(
        INotificationSender notificationSender,
        ILogger<EmailVerificationRequestedConsumer> logger,
        IConfiguration configuration)
    {
        _notificationSender = notificationSender;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<EmailVerificationRequestedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing email verification for {Email}", message.Email);

        try
        {
            var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:3000";
            var verificationUrl = $"{baseUrl}/verify-email?token={message.VerificationToken}";

            var variables = new Dictionary<string, string>
            {
                { "name", message.Email.Split('@')[0] },
                { "email", message.Email },
                { "verification_url", verificationUrl }
            };

            await _notificationSender.SendNotificationAsync(
                message.UserId,
                null,
                NotificationTypes.EmailVerification,
                message.Email,
                variables,
                context.CancellationToken);

            _logger.LogInformation("Email verification sent to {Email}", message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to {Email}", message.Email);
            throw;
        }
    }
}
