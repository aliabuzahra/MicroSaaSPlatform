using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Tenant.Service.Domain.Entities;

public class Tenant : Entity<Guid>, IAggregateRoot
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string SubscriptionPlan { get; set; } = "Free";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Contact info (simplified)
    public string? ContactEmail { get; set; }

    public static Result<Tenant> Create(string name, string slug, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Tenant>("Name is required.");
        
        if (string.IsNullOrWhiteSpace(slug))
            return Result.Failure<Tenant>("Slug is required.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            ContactEmail = email
        };

        return Result.Success(tenant);
    }
    
    public void UpgradePlan(string newPlan)
    {
        SubscriptionPlan = newPlan;
    }
}
