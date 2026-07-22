using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Tenant.Service.Domain.Entities;

public class TenantSettings : Entity<Guid>
{
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public static TenantSettings Create(Guid tenantId, string key, string value)
    {
        return new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Key = key,
            Value = value
        };
    }
}
