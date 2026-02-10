using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Billing.Service.Domain.Entities;

public class PlanLimit : Entity<Guid>, IAggregateRoot
{
    public string PlanId { get; private set; } // Paddle Price ID or Internal ID
    public string MetricKey { get; private set; }
    public decimal MaxQuantity { get; private set; }
    public string Period { get; private set; } // "monthly", "lifetime"

    private PlanLimit() { } // EF Core

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public PlanLimit(string planId, string metricKey, decimal maxQuantity, string period = "monthly")
    {
        Id = Guid.NewGuid();
        PlanId = planId;
        MetricKey = metricKey;
        MaxQuantity = maxQuantity;
        Period = period;
    }
}
