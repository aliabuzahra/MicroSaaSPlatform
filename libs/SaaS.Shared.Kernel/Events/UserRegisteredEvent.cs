using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record UserRegisteredEvent(Guid UserId, string Email, string Name) : IDomainEvent;
