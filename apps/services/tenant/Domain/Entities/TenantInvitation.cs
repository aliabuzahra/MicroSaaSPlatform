using SaaS.Shared.Kernel.BuildingBlocks;
using System.Security.Cryptography;

namespace SaaS.Tenant.Service.Domain.Entities;

public class TenantInvitation : Entity<Guid>
{
    public required Guid TenantId { get; set; }
    public required string Email { get; set; }
    public required string Token { get; set; }
    public string Role { get; set; } = "Member";
    public Guid InvitedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsAccepted => AcceptedAt != null;
    public bool IsDeclined => DeclinedAt != null;
    public bool IsPending => !IsExpired && !IsAccepted && !IsDeclined;

    public static TenantInvitation Create(Guid tenantId, string email, string role, Guid invitedByUserId, int expirationDays = 7)
    {
        return new TenantInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.ToLowerInvariant(),
            Token = GenerateToken(),
            Role = role,
            InvitedByUserId = invitedByUserId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
        };
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public void Accept()
    {
        AcceptedAt = DateTime.UtcNow;
    }

    public void Decline()
    {
        DeclinedAt = DateTime.UtcNow;
    }
}
