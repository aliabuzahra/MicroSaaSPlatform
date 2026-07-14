using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;

namespace SaaS.Audit.Service.Consumers;

public class TenantMemberAddedConsumer : IConsumer<TenantMemberAddedEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<TenantMemberAddedConsumer> _logger;

    public TenantMemberAddedConsumer(IAuditLogger auditLogger, ILogger<TenantMemberAddedConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TenantMemberAddedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing audit for tenant member added: {Email} to {TenantId}", 
            message.Email, message.TenantId);

        await _auditLogger.LogAsync(
            eventType: AuditEventTypes.TenantMemberAdded,
            action: AuditActions.Create,
            userId: message.UserId,
            userEmail: message.Email,
            tenantId: message.TenantId,
            resourceType: AuditResourceTypes.TenantMember,
            resourceId: $"{message.TenantId}/{message.UserId}",
            newValues: new { message.Email, message.Role },
            severity: AuditSeverity.Info,
            cancellationToken: context.CancellationToken);

        _logger.LogInformation("Audit log created for tenant member added: {Email}", message.Email);
    }
}
