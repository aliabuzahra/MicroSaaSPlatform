using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;

namespace SaaS.Audit.Service.Consumers;

public class TenantCreatedConsumer : IConsumer<TenantCreatedEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<TenantCreatedConsumer> _logger;

    public TenantCreatedConsumer(IAuditLogger auditLogger, ILogger<TenantCreatedConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TenantCreatedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing audit for tenant creation: {TenantName}", message.Name);

        await _auditLogger.LogAsync(
            eventType: AuditEventTypes.TenantCreated,
            action: AuditActions.Create,
            tenantId: message.TenantId,
            resourceType: AuditResourceTypes.Tenant,
            resourceId: message.TenantId.ToString(),
            newValues: new { message.Name, message.Slug, message.ContactEmail },
            severity: AuditSeverity.Info,
            cancellationToken: context.CancellationToken);

        _logger.LogInformation("Audit log created for tenant creation: {TenantName}", message.Name);
    }
}
