namespace SaaS.Shared.Kernel.BuildingBlocks;

public interface ITenantContext
{
    TenantId TenantId { get; }
}
