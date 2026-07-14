using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Features.Service.Domain.Entities;

public class FeatureFlag : Entity<Guid>
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = false;
    public FeatureFlagType Type { get; set; } = FeatureFlagType.Boolean;
    public string? DefaultValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static FeatureFlag Create(string key, string name, string? description = null, bool isEnabled = false)
    {
        return new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key.ToLowerInvariant().Replace(" ", "_"),
            Name = name,
            Description = description,
            IsEnabled = isEnabled,
            Type = FeatureFlagType.Boolean,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string? name, string? description, bool? isEnabled)
    {
        if (name != null) Name = name;
        if (description != null) Description = description;
        if (isEnabled.HasValue) IsEnabled = isEnabled.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class PlanFeature : Entity<Guid>
{
    public required string PlanId { get; set; }
    public required string FeatureKey { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Value { get; set; }
    public int? Limit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static PlanFeature Create(string planId, string featureKey, bool isEnabled = true, string? value = null, int? limit = null)
    {
        return new PlanFeature
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            FeatureKey = featureKey,
            IsEnabled = isEnabled,
            Value = value,
            Limit = limit,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class TenantFeatureOverride : Entity<Guid>, IMustHaveTenant
{
    public TenantId TenantId { get; set; } = TenantId.Empty;
    public required string FeatureKey { get; set; }
    public bool IsEnabled { get; set; }
    public string? Value { get; set; }
    public int? Limit { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    public static TenantFeatureOverride Create(
        Guid tenantId,
        string featureKey,
        bool isEnabled,
        string? value = null,
        int? limit = null,
        string? reason = null,
        DateTime? expiresAt = null)
    {
        return new TenantFeatureOverride
        {
            Id = Guid.NewGuid(),
            TenantId = new TenantId(tenantId),
            FeatureKey = featureKey,
            IsEnabled = isEnabled,
            Value = value,
            Limit = limit,
            Reason = reason,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum FeatureFlagType
{
    Boolean,
    String,
    Number,
    Json
}

public static class StandardFeatures
{
    public const string ApiAccess = "api_access";
    public const string WebhooksEnabled = "webhooks_enabled";
    public const string CustomDomain = "custom_domain";
    public const string SsoEnabled = "sso_enabled";
    public const string AuditLogs = "audit_logs";
    public const string AdvancedAnalytics = "advanced_analytics";
    public const string PrioritySupport = "priority_support";
    public const string MaxUsers = "max_users";
    public const string MaxApiKeys = "max_api_keys";
    public const string MaxWebhooks = "max_webhooks";
    public const string MaxStorage = "max_storage_gb";
    
    public static readonly string[] All = [
        ApiAccess, WebhooksEnabled, CustomDomain, SsoEnabled, AuditLogs,
        AdvancedAnalytics, PrioritySupport, MaxUsers, MaxApiKeys, MaxWebhooks, MaxStorage
    ];
}
