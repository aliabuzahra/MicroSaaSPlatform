using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record TenantUpdatedEvent(Guid TenantId, string Name, string? ContactEmail, string SubscriptionPlan) : IDomainEvent;
