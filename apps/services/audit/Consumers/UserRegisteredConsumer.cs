using System.Text.Json;
using MassTransit;
using SaaS.Shared.Kernel.Events;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace SaaS.Audit.Service.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private readonly AuditDbContext _db;

    public UserRegisteredConsumer(ILogger<UserRegisteredConsumer> logger, AuditDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation("Audit Log: User registered with Email: {Email} (ID: {Id})", evt.Email, evt.UserId);

        var auditLog = AuditLog.Create(
            tenantId: Guid.Empty,
            action: "UserRegistered",
            entityType: "User",
            entityId: evt.UserId.ToString(),
            userEmail: evt.Email,
            details: JsonSerializer.Serialize(new { evt.Email, evt.Name })
        );

        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync();
    }
}
