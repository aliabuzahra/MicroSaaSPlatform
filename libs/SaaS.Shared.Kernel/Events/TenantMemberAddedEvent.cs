using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record TenantMemberAddedEvent(Guid TenantId, Guid UserId, string Email, string Role) : IDomainEvent;
