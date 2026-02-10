using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Billing.Service.Domain.Entities;

public class UsageRecord : Entity<Guid>, IAggregateRoot
{
    public Guid TenantId { get; private set; }
    public string MetricKey { get; private set; } // e.g., "storage_gb", "api_calls"
    public decimal Quantity { get; private set; }
    public DateTime Timestamp { get; private set; }

    private UsageRecord() { } // EF Core

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public UsageRecord(Guid tenantId, string metricKey, decimal quantity)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        MetricKey = metricKey;
        Quantity = quantity;
        Timestamp = DateTime.UtcNow;
    }
}
