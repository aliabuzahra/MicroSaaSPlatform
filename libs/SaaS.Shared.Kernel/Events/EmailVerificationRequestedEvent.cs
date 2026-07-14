using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record EmailVerificationRequestedEvent(Guid UserId, string Email, string VerificationToken) : IDomainEvent;
