namespace SaaS.Shared.Kernel.Events;

public record UserLoggedInEvent(
    Guid UserId,
    string Email,
    string? IpAddress,
    string? UserAgent,
    DateTime LoggedInAt
);
