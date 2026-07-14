using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Events;

public record TenantCreatedEvent(Guid TenantId, string Name, string Slug, string? ContactEmail) : IDomainEvent;
