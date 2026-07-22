using MassTransit;
using SaaS.Shared.Kernel.IntegrationEvents.Billing;
using SaaS.Notifications.Service.Services;
using Microsoft.Extensions.Logging;

namespace SaaS.Notifications.Service.Consumers;

public class SubscriptionStatusChangedConsumer : IConsumer<SubscriptionStatusChanged>
{
    private readonly ILogger<SubscriptionStatusChangedConsumer> _logger;
    private readonly NotificationService _notificationService;

    public SubscriptionStatusChangedConsumer(
        ILogger<SubscriptionStatusChangedConsumer> logger, 
        NotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<SubscriptionStatusChanged> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation("Subscription status changed for Tenant {TenantId}: {Status}", evt.TenantId, evt.NewStatus);

        var templateKey = evt.NewStatus switch
        {
            "active" => "subscription_activated",
            "canceled" => "subscription_cancelled",
            _ => null
        };

        if (templateKey != null)
        {
            await _notificationService.SendTemplatedEmailAsync(
                templateKey,
                $"tenant-{evt.TenantId}@notifications.local",
                new Dictionary<string, string>
                {
                    { "TenantId", evt.TenantId.ToString() },
                    { "PlanId", evt.PlanId },
                    { "NextBillDate", evt.NextBillDate?.ToString("yyyy-MM-dd") ?? "N/A" },
                    { "Status", evt.NewStatus }
                },
                evt.TenantId
            );
        }
    }
}
