using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Billing.Service.Domain.Entities;

public class BillingPlan : Entity<Guid>, IAggregateRoot
{
    public required string Name { get; set; }
    public required string StripePriceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string Interval { get; set; } = "month"; // month, year

    public static BillingPlan Create(string name, string stripePriceId, decimal amount)
    {
        return new BillingPlan
        {
            Id = Guid.NewGuid(),
            Name = name,
            StripePriceId = stripePriceId,
            Amount = amount
        };
    }
}
