using System;

namespace SaaS.Shared.Kernel.IntegrationEvents.Billing;

public record SubscriptionStatusChanged(
    Guid TenantId,
    string NewStatus, // e.g., "active", "canceled", "past_due"
    string PlanId,
    DateTime? NextBillDate,
    DateTime OccurredOn
);
