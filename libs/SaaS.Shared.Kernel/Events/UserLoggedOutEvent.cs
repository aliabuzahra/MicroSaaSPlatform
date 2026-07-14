namespace SaaS.Shared.Kernel.Events;

public record UserLoggedOutEvent(
    Guid UserId,
    DateTime LoggedOutAt
);
