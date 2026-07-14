using SaaS.Shared.Kernel.BuildingBlocks;
using System.Security.Cryptography;

namespace SaaS.ApiKeys.Service.Domain.Entities;

public class ApiKey : Entity<Guid>, IMustHaveTenant
{
    public required string Name { get; set; }
    public required string KeyHash { get; set; }
    public required string KeyPrefix { get; set; }
    public TenantId TenantId { get; set; } = TenantId.Empty;
    public Guid CreatedByUserId { get; set; }
    public List<string> Scopes { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public bool IsActive => RevokedAt == null && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

    public static (ApiKey key, string rawKey) Create(
        string name,
        Guid tenantId,
        Guid createdByUserId,
        List<string>? scopes = null,
        DateTime? expiresAt = null)
    {
        var rawKey = GenerateApiKey();
        var keyPrefix = rawKey[..8];
        var keyHash = HashKey(rawKey);

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            TenantId = new TenantId(tenantId),
            CreatedByUserId = createdByUserId,
            Scopes = scopes ?? new List<string> { "read", "write" },
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        return (apiKey, rawKey);
    }

    public bool ValidateKey(string rawKey)
    {
        if (!IsActive) return false;
        return HashKey(rawKey) == KeyHash;
    }

    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public void Revoke(string? reason = null)
    {
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"sk_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }

    private static string HashKey(string key)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

public static class ApiKeyScopes
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Admin = "admin";
    public const string Billing = "billing";
    public const string Webhooks = "webhooks";
    
    public static readonly string[] All = [Read, Write, Admin, Billing, Webhooks];
    
    public static bool IsValid(string scope) => All.Contains(scope);
}
