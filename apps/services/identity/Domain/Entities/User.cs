using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Identity.Service.Domain.Entities;

public class User : Entity<Guid>, IAggregateRoot, IMustHaveTenant
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public TenantId TenantId { get; set; } = TenantId.Empty; // Default for now, should be required

    public static Result<User> Create(string email, string passwordHash, string fullName, TenantId tenantId)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>("Email is required.");
            
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>("Password hash is required.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            TenantId = tenantId
        };

        // Could add a Domain Event here, e.g., user.AddDomainEvent(new UserRegisteredEvent(user.Id));

        return Result.Success(user);
    }
}
