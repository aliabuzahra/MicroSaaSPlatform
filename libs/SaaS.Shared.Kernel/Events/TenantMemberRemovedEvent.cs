using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record TenantMemberRemovedEvent(Guid TenantId, Guid UserId) : IDomainEvent;
