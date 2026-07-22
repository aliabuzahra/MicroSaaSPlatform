using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Identity.Service.Domain.Entities;

public class RefreshToken : Entity<Guid>
{
    public required string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public static RefreshToken Create(Guid userId, string token, int expirationDays)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
        };
    }

    public void Revoke(string? replacementToken = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacementToken;
    }
}
