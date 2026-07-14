using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Identity.Service.Domain.Entities;

public class PasswordResetToken : Entity<Guid>
{
    public required string Token { get; set; }
    public required Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsUsed => UsedAt != null;
    public bool IsValid => !IsExpired && !IsUsed;

    public static PasswordResetToken Create(Guid userId, int expirationHours = 24)
    {
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            Token = GenerateToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public void MarkAsUsed()
    {
        UsedAt = DateTime.UtcNow;
    }
}
