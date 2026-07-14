using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record PasswordResetRequestedEvent(Guid UserId, string Email, string ResetToken) : IDomainEvent;
