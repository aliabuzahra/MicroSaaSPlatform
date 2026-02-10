namespace SaaS.Shared.Kernel.BuildingBlocks;

public interface IMustHaveTenant
{
    TenantId TenantId { get; }
}
