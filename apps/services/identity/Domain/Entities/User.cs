using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Identity.Service.Domain.Entities;

public class User : Entity<Guid>, IAggregateRoot, IMustHaveTenant
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    public TenantId TenantId { get; set; } = TenantId.Empty;

    public static Result<User> Create(string email, string passwordHash, string fullName, TenantId tenantId)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>("Email is required.");
            
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>("Password hash is required.");

        if (!IsValidEmail(email))
            return Result.Failure<User>("Invalid email format.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName,
            TenantId = tenantId
        };

        return Result.Success(user);
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        EmailVerified = true;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
