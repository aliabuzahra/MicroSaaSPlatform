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

    public async Task Consume(ConsumeContext<SubscriptionStatusChanged> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing subscription status change for tenant {TenantId}: {Status}", 
            message.TenantId, message.NewStatus);

        try
        {
            if (!string.IsNullOrEmpty(message.ContactEmail))
            {
                await _notificationSender.SendNotificationAsync(
                    userId: Guid.Empty,
                    tenantId: message.TenantId,
                    notificationType: NotificationTypes.SubscriptionUpdated,
                    recipient: message.ContactEmail,
                    variables: new Dictionary<string, string>
                    {
                        { "name", message.TenantName ?? "Customer" },
                        { "status", message.NewStatus },
                        { "plan", message.PlanId },
                        { "next_bill_date", message.NextBillDate?.ToString("MMMM dd, yyyy") ?? "N/A" }
                    });

                _logger.LogInformation(
                    "Subscription notification sent to {Email} for tenant {TenantId}. Status: {Status}, Plan: {Plan}",
                    message.ContactEmail, message.TenantId, message.NewStatus, message.PlanId);
            }
            else
            {
                _logger.LogWarning(
                    "No contact email provided for subscription status change notification. Tenant: {TenantId}",
                    message.TenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process subscription status change for tenant {TenantId}", message.TenantId);
            throw;
        }
    }
}
