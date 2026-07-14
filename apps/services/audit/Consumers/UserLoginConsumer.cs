using MassTransit;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure;
using SaaS.Shared.Kernel.Events;

namespace SaaS.Audit.Service.Consumers;

public class UserLoginConsumer : IConsumer<UserLoggedInEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UserLoginConsumer> _logger;

    public UserLoginConsumer(IAuditLogger auditLogger, ILogger<UserLoginConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserLoggedInEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Auditing user login for {Email}", message.Email);

        await _auditLogger.LogAsync(
            eventType: "user.login",
            action: "Login",
            userId: message.UserId,
            userEmail: message.Email,
            resourceType: "User",
            resourceId: message.UserId.ToString(),
            metadata: new { ipAddress = message.IpAddress ?? "unknown", userAgent = message.UserAgent ?? "unknown" },
            severity: AuditSeverity.Info);
    }
}

public class UserLogoutConsumer : IConsumer<UserLoggedOutEvent>
{
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<UserLogoutConsumer> _logger;

    public UserLogoutConsumer(IAuditLogger auditLogger, ILogger<UserLogoutConsumer> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserLoggedOutEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Auditing user logout for {UserId}", message.UserId);

        await _auditLogger.LogAsync(
            eventType: "user.logout",
            action: "Logout",
            userId: message.UserId,
            resourceType: "User",
            resourceId: message.UserId.ToString(),
            severity: AuditSeverity.Info);
    }
}
