using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Notifications.Service.Services;
using Microsoft.Extensions.Logging;

namespace SaaS.Notifications.Service.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private readonly NotificationService _notificationService;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger, NotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation("Sending Welcome Email to {Email} (User: {Name})", evt.Email, evt.Name);

        await _notificationService.SendTemplatedEmailAsync(
            "welcome_email",
            evt.Email,
            new Dictionary<string, string>
            {
                { "Name", evt.Name },
                { "Email", evt.Email }
            }
        );
    }
}
