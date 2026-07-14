using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Email;

namespace SaaS.Notifications.Service.Consumers;

public class PasswordResetRequestedConsumer : IConsumer<PasswordResetRequestedEvent>
{
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<PasswordResetRequestedConsumer> _logger;
    private readonly IConfiguration _configuration;

    public PasswordResetRequestedConsumer(
        INotificationSender notificationSender,
        ILogger<PasswordResetRequestedConsumer> logger,
        IConfiguration configuration)
    {
        _notificationSender = notificationSender;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Consume(ConsumeContext<PasswordResetRequestedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing password reset email for {Email}", message.Email);

        try
        {
            var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:3000";
            var resetUrl = $"{baseUrl}/reset-password?token={message.ResetToken}";

            var variables = new Dictionary<string, string>
            {
                { "name", message.Email.Split('@')[0] },
                { "email", message.Email },
                { "reset_url", resetUrl }
            };

            await _notificationSender.SendNotificationAsync(
                message.UserId,
                null,
                NotificationTypes.PasswordReset,
                message.Email,
                variables,
                context.CancellationToken);

            _logger.LogInformation("Password reset email sent to {Email}", message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", message.Email);
            throw;
        }
    }
}
