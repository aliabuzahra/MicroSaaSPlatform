using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Tenant.Service.Domain.Entities;

public class TenantMember : Entity<Guid>
{
    public required Guid TenantId { get; set; }
    public required Guid UserId { get; set; }
    public required string Email { get; set; }
    public string Role { get; set; } = "Member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public static TenantMember Create(Guid tenantId, Guid userId, string email, string role = "Member")
    {
        return new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Email = email.ToLowerInvariant(),
            Role = role,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdateRole(string newRole)
    {
        Role = newRole;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public static class TenantRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Member = "Member";
    
    public static readonly string[] All = [Owner, Admin, Member];
    
    public static bool IsValid(string role) => All.Contains(role);
}
