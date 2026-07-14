using MassTransit;
using SaaS.Shared.Kernel.IntegrationEvents.Billing;
using SaaS.Notifications.Service.Domain.Entities;
using SaaS.Notifications.Service.Infrastructure.Email;

namespace SaaS.Notifications.Service.Consumers;

public class SubscriptionStatusChangedConsumer : IConsumer<SubscriptionStatusChanged>
{
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<SubscriptionStatusChangedConsumer> _logger;

    public SubscriptionStatusChangedConsumer(
        INotificationSender notificationSender,
        ILogger<SubscriptionStatusChangedConsumer> logger)
    {
        _notificationSender = notificationSender;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<SubscriptionStatusChanged> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing subscription status change for tenant {TenantId}: {Status}", 
            message.TenantId, message.NewStatus);

        try
        {
            _logger.LogInformation(
                "Subscription status changed for tenant {TenantId}. Status: {Status}, Plan: {Plan}, Next Bill: {NextBill}",
                message.TenantId, message.NewStatus, message.PlanId, message.NextBillDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process subscription status change for tenant {TenantId}", message.TenantId);
            throw;
        }

        return Task.CompletedTask;
    }
}
