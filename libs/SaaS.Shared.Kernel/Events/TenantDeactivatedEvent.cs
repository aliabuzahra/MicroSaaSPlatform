using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record TenantDeactivatedEvent(Guid TenantId, string Name) : IDomainEvent;
