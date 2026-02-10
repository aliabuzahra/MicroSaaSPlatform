using MassTransit;
using SaaS.Shared.Kernel.Events;
using Microsoft.Extensions.Logging;

namespace SaaS.Audit.Service.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        _logger.LogInformation("Audit Log: User registered with Email: {Email} (ID: {Id})", context.Message.Email, context.Message.UserId);
        return Task.CompletedTask;
    }
}
