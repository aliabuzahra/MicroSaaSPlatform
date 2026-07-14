using MassTransit;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;
using SaaS.Shared.Kernel.Events;

namespace SaaS.Audit.Service.Consumers;

public class TenantInvitationSentConsumer : IConsumer<TenantInvitationSentEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<TenantInvitationSentConsumer> _logger;

    public TenantInvitationSentConsumer(IAuditLogger auditLogger, ILogger<TenantInvitationSentConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TenantInvitationSentEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Auditing tenant invitation sent event for {Email}", message.Email);

        await _auditLogger.LogAsync(
            eventType: "tenant.invitation.sent",
            action: "Create",
            userId: message.InvitedByUserId,
            tenantId: message.TenantId,
            resourceType: "TenantInvitation",
            resourceId: message.InvitationId.ToString(),
            metadata: new { email = message.Email, role = message.Role },
            severity: AuditSeverity.Info);
    }
}
