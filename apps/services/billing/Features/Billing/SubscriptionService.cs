using Microsoft.EntityFrameworkCore;
using MassTransit;
using SaaS.Billing.Service.Domain.Entities;
using SaaS.Billing.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.IntegrationEvents.Billing;

namespace SaaS.Billing.Service.Features.Billing;

public class SubscriptionService
{
    private readonly BillingDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(BillingDbContext db, IPublishEndpoint publishEndpoint, ILogger<SubscriptionService> logger)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task HandleSubscriptionUpdateAsync(Guid tenantId, string status, string planId, string paddleSubscriptionId, DateTime? nextBillDate, string? updateUrl, string? cancelUrl)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (subscription == null)
        {
            // Create new
            subscription = Subscription.Create(tenantId, planId, paddleSubscriptionId, nextBillDate ?? DateTime.UtcNow.AddMonths(1), updateUrl, cancelUrl);
            subscription.Status = status;
            _db.Subscriptions.Add(subscription);
        }
        else
        {
            // Update existing
            subscription.Status = status;
            subscription.PlanId = planId;
            subscription.PaddleSubscriptionId = paddleSubscriptionId;
            // Only update URLs if provided
            if (!string.IsNullOrEmpty(updateUrl)) subscription.UpdateUrl = updateUrl;
            if (!string.IsNullOrEmpty(cancelUrl)) subscription.CancelUrl = cancelUrl;
            if (nextBillDate.HasValue) subscription.CurrentPeriodEnd = nextBillDate.Value;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Subscription for Tenant {TenantId} updated to {Status}", tenantId, status);

        await _publishEndpoint.Publish(new SubscriptionStatusChanged(
            TenantId: tenantId,
            NewStatus: status,
            PlanId: planId,
            NextBillDate: nextBillDate,
            OccurredOn: DateTime.UtcNow
        ));
    }
}
