using SaaS.Shared.Kernel.BuildingBlocks;
using System.Text.RegularExpressions;

namespace SaaS.Tenant.Service.Domain.Entities;

public class Tenant : Entity<Guid>, IAggregateRoot
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string SubscriptionPlan { get; set; } = "Free";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? ContactEmail { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }

    public static Result<Tenant> Create(string name, string slug, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Tenant>("Name is required.");
        
        if (string.IsNullOrWhiteSpace(slug))
            return Result.Failure<Tenant>("Slug is required.");

        if (!IsValidSlug(slug))
            return Result.Failure<Tenant>("Slug can only contain lowercase letters, numbers, and hyphens.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            ContactEmail = email
        };

        return Result.Success(tenant);
    }

    public Result Update(string? name, string? email, string? logoUrl, string? description)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure("Name cannot be empty.");
            Name = name;
        }

        if (email is not null)
            ContactEmail = email;

        if (logoUrl is not null)
            LogoUrl = logoUrl;

        if (description is not null)
            Description = description;

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
    
    public void UpgradePlan(string newPlan)
    {
        SubscriptionPlan = newPlan;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsValidSlug(string slug)
    {
        return Regex.IsMatch(slug, @"^[a-z0-9]+(?:-[a-z0-9]+)*$");
    }
}
