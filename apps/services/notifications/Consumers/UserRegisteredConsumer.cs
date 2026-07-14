using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Email;

namespace SaaS.Notifications.Service.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(
        INotificationSender notificationSender,
        ILogger<UserRegisteredConsumer> logger)
    {
        _notificationSender = notificationSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing welcome email for {Email} (User: {Name})", message.Email, message.Name);

        try
        {
            var variables = new Dictionary<string, string>
            {
                { "name", message.Name },
                { "email", message.Email }
            };

            await _notificationSender.SendNotificationAsync(
                message.UserId,
                null,
                NotificationTypes.WelcomeEmail,
                message.Email,
                variables,
                context.CancellationToken);

            _logger.LogInformation("Welcome email sent to {Email}", message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", message.Email);
            throw;
        }
    }
}
