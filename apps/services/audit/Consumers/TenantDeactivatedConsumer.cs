using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;

namespace SaaS.Audit.Service.Consumers;

public class TenantDeactivatedConsumer : IConsumer<TenantDeactivatedEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<TenantDeactivatedConsumer> _logger;

    public TenantDeactivatedConsumer(IAuditLogger auditLogger, ILogger<TenantDeactivatedConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TenantDeactivatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing audit for tenant deactivation: {TenantId}", message.TenantId);

        await _auditLogger.LogAsync(
            eventType: AuditEventTypes.TenantDeactivated,
            action: AuditActions.Delete,
            tenantId: message.TenantId,
            resourceType: AuditResourceTypes.Tenant,
            resourceId: message.TenantId.ToString(),
            newValues: new { message.Name, IsActive = false },
            severity: AuditSeverity.Warning,
            cancellationToken: context.CancellationToken);

        _logger.LogInformation("Audit log created for tenant deactivation: {TenantId}", message.TenantId);
    }
}
