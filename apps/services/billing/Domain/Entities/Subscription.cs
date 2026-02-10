using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Billing.Service.Domain.Entities;

public class Subscription : Entity<Guid>, IAggregateRoot
{
    public Guid TenantId { get; private set; }
    public string PlanId { get; set; } // Paddle Price ID (string)
    public required string PaddleSubscriptionId { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CurrentPeriodEnd { get; set; }
    public string? UpdateUrl { get; set; }
    public string? CancelUrl { get; set; }

    private Subscription() { } // EF Core

    public static Subscription Create(Guid tenantId, string planId, string paddleSubscriptionId, DateTime currentPeriodEnd, string? updateUrl = null, string? cancelUrl = null)
    {
        return new Subscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = planId,
            PaddleSubscriptionId = paddleSubscriptionId,
            CurrentPeriodEnd = currentPeriodEnd,
            UpdateUrl = updateUrl,
            CancelUrl = cancelUrl
        };
    }
}
