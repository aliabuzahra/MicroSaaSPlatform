using System.Text.Json;
using MassTransit;
using SaaS.Shared.Kernel.IntegrationEvents.Billing;
using SaaS.Audit.Service.Domain.Entities;
using SaaS.Audit.Service.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace SaaS.Audit.Service.Consumers;

public class SubscriptionStatusChangedConsumer : IConsumer<SubscriptionStatusChanged>
{
    private readonly ILogger<SubscriptionStatusChangedConsumer> _logger;
    private readonly AuditDbContext _db;

    public SubscriptionStatusChangedConsumer(ILogger<SubscriptionStatusChangedConsumer> logger, AuditDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task Consume(ConsumeContext<SubscriptionStatusChanged> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation("Audit Log: Subscription status changed for Tenant {TenantId}: {NewStatus}", evt.TenantId, evt.NewStatus);

        var auditLog = AuditLog.Create(
            tenantId: evt.TenantId,
            action: "SubscriptionStatusChanged",
            entityType: "Subscription",
            entityId: evt.PlanId,
            details: JsonSerializer.Serialize(new { 
                evt.NewStatus, 
                evt.PlanId, 
                evt.NextBillDate,
                evt.OccurredOn 
            })
        );

        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync();
    }
}
