using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;

namespace SaaS.Audit.Service.Consumers;

public class TenantMemberRemovedConsumer : IConsumer<TenantMemberRemovedEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<TenantMemberRemovedConsumer> _logger;

    public TenantMemberRemovedConsumer(IAuditLogger auditLogger, ILogger<TenantMemberRemovedConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TenantMemberRemovedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing audit for tenant member removed: {UserId} from {TenantId}", 
            message.UserId, message.TenantId);

        await _auditLogger.LogAsync(
            eventType: AuditEventTypes.TenantMemberRemoved,
            action: AuditActions.Delete,
            userId: message.UserId,
            tenantId: message.TenantId,
            resourceType: AuditResourceTypes.TenantMember,
            resourceId: $"{message.TenantId}/{message.UserId}",
            severity: AuditSeverity.Warning,
            cancellationToken: context.CancellationToken);

        _logger.LogInformation("Audit log created for tenant member removed: {UserId}", message.UserId);
    }
}
