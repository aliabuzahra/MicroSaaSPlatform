using MassTransit;
using Microsoft.EntityFrameworkCore;
using SaaS.Shared.Kernel.IntegrationEvents.Billing;
using SaaS.Tenant.Service.Infrastructure.Persistence;

namespace SaaS.Tenant.Service.Consumers;

public class SubscriptionStatusChangedConsumer : IConsumer<SubscriptionStatusChanged>
{
    private readonly TenantDbContext _db;
    private readonly ILogger<SubscriptionStatusChangedConsumer> _logger;

    public SubscriptionStatusChangedConsumer(
        TenantDbContext db,
        ILogger<SubscriptionStatusChangedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubscriptionStatusChanged> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Syncing subscription status for tenant {TenantId}: Plan={Plan}, Status={Status}", 
            message.TenantId, message.PlanId, message.NewStatus);

        try
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == message.TenantId);
            
            if (tenant is null)
            {
                _logger.LogWarning("Tenant {TenantId} not found for subscription sync", message.TenantId);
                return;
            }

            tenant.UpgradePlan(message.PlanId);

            if (message.NewStatus == "canceled" || message.NewStatus == "expired")
            {
                tenant.UpgradePlan("free");
            }

            if (message.NewStatus == "suspended")
            {
                tenant.Deactivate();
            }
            else if (message.NewStatus == "active" && !tenant.IsActive)
            {
                tenant.Activate();
            }

            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Tenant {TenantId} subscription synced: Plan={Plan}, IsActive={IsActive}", 
                message.TenantId, tenant.SubscriptionPlan, tenant.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync subscription status for tenant {TenantId}", message.TenantId);
            throw;
        }
    }
}
