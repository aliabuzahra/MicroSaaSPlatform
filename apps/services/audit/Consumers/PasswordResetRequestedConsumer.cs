using MassTransit;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;
using SaaS.Shared.Kernel.Events;

namespace SaaS.Audit.Service.Consumers;

public class PasswordResetRequestedConsumer : IConsumer<PasswordResetRequestedEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<PasswordResetRequestedConsumer> _logger;

    public PasswordResetRequestedConsumer(IAuditLogger auditLogger, ILogger<PasswordResetRequestedConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PasswordResetRequestedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Auditing password reset requested for {Email}", message.Email);

        await _auditLogger.LogAsync(
            eventType: "user.password_reset.requested",
            action: "Request",
            userId: message.UserId,
            userEmail: message.Email,
            resourceType: "PasswordReset",
            resourceId: message.UserId.ToString(),
            severity: AuditSeverity.Warning);
    }
}
