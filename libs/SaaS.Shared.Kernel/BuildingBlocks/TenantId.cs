using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.BuildingBlocks;

public record TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());
    public static TenantId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
