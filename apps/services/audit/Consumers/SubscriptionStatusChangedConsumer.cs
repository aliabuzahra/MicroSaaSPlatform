using MassTransit;
using SaaS.Shared.Kernel.IntegrationEvents.Billing;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;

namespace SaaS.Audit.Service.Consumers;

public class SubscriptionStatusChangedConsumer : IConsumer<SubscriptionStatusChanged>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<SubscriptionStatusChangedConsumer> _logger;

    public SubscriptionStatusChangedConsumer(IAuditLogger auditLogger, ILogger<SubscriptionStatusChangedConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubscriptionStatusChanged> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing audit for subscription status change: {TenantId} - {Status}", 
            message.TenantId, message.NewStatus);

        var eventType = message.NewStatus.ToLower() switch
        {
            "active" => AuditEventTypes.SubscriptionCreated,
            "cancelled" or "canceled" => AuditEventTypes.SubscriptionCancelled,
            _ => AuditEventTypes.SubscriptionUpdated
        };

        await _auditLogger.LogAsync(
            eventType: eventType,
            action: AuditActions.Update,
            tenantId: message.TenantId,
            resourceType: AuditResourceTypes.Subscription,
            resourceId: message.TenantId.ToString(),
            newValues: new 
            { 
                Status = message.NewStatus, 
                PlanId = message.PlanId, 
                NextBillDate = message.NextBillDate 
            },
            severity: message.NewStatus.ToLower() == "cancelled" ? AuditSeverity.Warning : AuditSeverity.Info,
            cancellationToken: context.CancellationToken);

        _logger.LogInformation("Audit log created for subscription status change: {TenantId}", message.TenantId);
    }
}
