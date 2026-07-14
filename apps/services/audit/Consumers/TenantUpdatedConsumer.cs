using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;

namespace SaaS.Audit.Service.Consumers;

public class TenantUpdatedConsumer : IConsumer<TenantUpdatedEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<TenantUpdatedConsumer> _logger;

    public TenantUpdatedConsumer(IAuditLogger auditLogger, ILogger<TenantUpdatedConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TenantUpdatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing audit for tenant update: {TenantId}", message.TenantId);

        await _auditLogger.LogAsync(
            eventType: AuditEventTypes.TenantUpdated,
            action: AuditActions.Update,
            tenantId: message.TenantId,
            resourceType: AuditResourceTypes.Tenant,
            resourceId: message.TenantId.ToString(),
            newValues: new { message.Name, message.ContactEmail, message.SubscriptionPlan },
            severity: AuditSeverity.Info,
            cancellationToken: context.CancellationToken);

        _logger.LogInformation("Audit log created for tenant update: {TenantId}", message.TenantId);
    }
}
