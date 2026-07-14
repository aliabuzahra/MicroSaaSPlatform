using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record TenantInvitationSentEvent(
    Guid TenantId, 
    string TenantName, 
    string Email, 
    string InvitationToken,
    string Role) : IDomainEvent;
