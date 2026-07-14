using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;

namespace SaaS.Audit.Service.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(IAuditLogger auditLogger, ILogger<UserRegisteredConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Processing audit for user registration: {Email}", message.Email);

        await _auditLogger.LogAsync(
            eventType: AuditEventTypes.UserRegistered,
            action: AuditActions.Create,
            userId: message.UserId,
            userEmail: message.Email,
            resourceType: AuditResourceTypes.User,
            resourceId: message.UserId.ToString(),
            newValues: new { message.Email, message.Name },
            severity: AuditSeverity.Info,
            cancellationToken: context.CancellationToken);

        _logger.LogInformation("Audit log created for user registration: {Email}", message.Email);
    }
}
