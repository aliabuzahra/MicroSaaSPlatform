using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Config.Service.Domain.Entities;

public class TenantSettings : Entity<Guid>, IMustHaveTenant
{
    public TenantId TenantId { get; set; } = TenantId.Empty;
    public required string Key { get; set; }
    public required string Value { get; set; }
    public SettingType Type { get; set; } = SettingType.String;
    public bool IsSecret { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static TenantSettings Create(Guid tenantId, string key, string value, SettingType type = SettingType.String, bool isSecret = false)
    {
        return new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = new TenantId(tenantId),
            Key = key,
            Value = value,
            Type = type,
            IsSecret = isSecret,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string value)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class GlobalSettings : Entity<Guid>
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public SettingType Type { get; set; } = SettingType.String;
    public string? Description { get; set; }
    public bool IsSecret { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static GlobalSettings Create(string key, string value, SettingType type = SettingType.String, string? description = null, bool isSecret = false)
    {
        return new GlobalSettings
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            Type = type,
            Description = description,
            IsSecret = isSecret,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum SettingType
{
    String,
    Number,
    Boolean,
    Json
}

public static class StandardSettings
{
    public const string Timezone = "timezone";
    public const string Locale = "locale";
    public const string DateFormat = "date_format";
    public const string Currency = "currency";
    public const string Theme = "theme";
    public const string PrimaryColor = "primary_color";
    public const string LogoUrl = "logo_url";
    public const string FaviconUrl = "favicon_url";
    public const string CompanyName = "company_name";
    public const string SupportEmail = "support_email";
    public const string PrivacyPolicyUrl = "privacy_policy_url";
    public const string TermsOfServiceUrl = "terms_of_service_url";
}
