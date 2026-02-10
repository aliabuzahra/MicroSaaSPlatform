using MassTransit;
using SaaS.Shared.Kernel.Events;
using Microsoft.Extensions.Logging;

namespace SaaS.Notifications.Service.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        _logger.LogInformation("EMAIL SIMULATION: Sending Welcome Email to {Email} (User: {Name})", context.Message.Email, context.Message.Name);
        // Simulate email sending delay
        return Task.Delay(100);
    }
}
