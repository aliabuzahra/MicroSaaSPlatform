using MassTransit;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;
using SaaS.Shared.Kernel.Events;

namespace SaaS.Audit.Service.Consumers;

public class EmailVerificationRequestedConsumer : IConsumer<EmailVerificationRequestedEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<EmailVerificationRequestedConsumer> _logger;

    public EmailVerificationRequestedConsumer(IAuditLogger auditLogger, ILogger<EmailVerificationRequestedConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmailVerificationRequestedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Auditing email verification requested for {Email}", message.Email);

        await _auditLogger.LogAsync(
            eventType: "user.email_verification.requested",
            action: "Request",
            userId: message.UserId,
            userEmail: message.Email,
            resourceType: "EmailVerification",
            resourceId: message.UserId.ToString(),
            severity: AuditSeverity.Info);
    }
}
