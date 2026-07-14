using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Webhooks.Service.Domain.Entities;
using SaaS.Webhooks.Service.Services;

namespace SaaS.Webhooks.Service.Consumers;

public class TenantCreatedWebhookConsumer : IConsumer<TenantCreatedEvent>
{
    private readonly IWebhookDeliveryService _deliveryService;
    private readonly ILogger<TenantCreatedWebhookConsumer> _logger;

    public TenantCreatedWebhookConsumer(
        IWebhookDeliveryService deliveryService,
        ILogger<TenantCreatedWebhookConsumer> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TenantCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing webhook for tenant created: {TenantId}", message.TenantId);

        await _deliveryService.DeliverWebhookAsync(
            message.TenantId,
            WebhookEventTypes.TenantCreated,
            new
            {
                TenantId = message.TenantId,
                Name = message.Name,
                Slug = message.Slug,
                ContactEmail = message.ContactEmail,
                Timestamp = DateTime.UtcNow
            },
            context.CancellationToken);
    }
}

public class TenantMemberAddedWebhookConsumer : IConsumer<TenantMemberAddedEvent>
{
    private readonly IWebhookDeliveryService _deliveryService;
    private readonly ILogger<TenantMemberAddedWebhookConsumer> _logger;

    public TenantMemberAddedWebhookConsumer(
        IWebhookDeliveryService deliveryService,
        ILogger<TenantMemberAddedWebhookConsumer> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TenantMemberAddedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing webhook for member added: {Email} to {TenantId}", 
            message.Email, message.TenantId);

        await _deliveryService.DeliverWebhookAsync(
            message.TenantId,
            WebhookEventTypes.TenantMemberAdded,
            new
            {
                TenantId = message.TenantId,
                UserId = message.UserId,
                Email = message.Email,
                Role = message.Role,
                Timestamp = DateTime.UtcNow
            },
            context.CancellationToken);
    }
}

public class UserRegisteredWebhookConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IWebhookDeliveryService _deliveryService;
    private readonly ILogger<UserRegisteredWebhookConsumer> _logger;

    public UserRegisteredWebhookConsumer(
        IWebhookDeliveryService deliveryService,
        ILogger<UserRegisteredWebhookConsumer> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing webhook for user registered: {Email}", message.Email);

        // Note: We don't have tenant info in UserRegisteredEvent directly
        // In a real system, you'd look up the user's tenant
    }
}
