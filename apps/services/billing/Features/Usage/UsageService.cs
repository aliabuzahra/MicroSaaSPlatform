using Microsoft.EntityFrameworkCore;
using SaaS.Billing.Service.Domain.Entities;
using SaaS.Billing.Service.Infrastructure.Persistence;
using SaaS.Shared.Kernel.Infrastructure.Caching;

namespace SaaS.Billing.Service.Features.Usage;

public class UsageService
{
    private readonly BillingDbContext _db;
    private readonly ILogger<UsageService> _logger;
    private readonly ICacheService _cache;

    public UsageService(BillingDbContext db, ILogger<UsageService> logger, ICacheService cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }

    public async Task TrackUsageAsync(Guid tenantId, string metricKey, decimal quantity)
    {
        var record = new UsageRecord(tenantId, metricKey, quantity);
        _db.UsageRecords.Add(record);
        await _db.SaveChangesAsync();
        
        // Invalidate usage cache
        await _cache.RemoveAsync($"usage:{tenantId}:{metricKey}");
        
        _logger.LogInformation("Tracked usage for Tenant {TenantId}: {Metric} +{Quantity}", tenantId, metricKey, quantity);
    }

    public async Task<decimal> GetUsageAsync(Guid tenantId, string metricKey, DateTime since)
    {
        var cacheKey = $"usage:{tenantId}:{metricKey}";
        var cached = await _cache.GetAsync<decimal?>(cacheKey);
        if (cached.HasValue) return cached.Value;

        var usage = await _db.UsageRecords
            .Where(r => r.TenantId == tenantId && r.MetricKey == metricKey && r.Timestamp >= since)
            .SumAsync(r => r.Quantity);

        await _cache.SetAsync(cacheKey, usage, TimeSpan.FromMinutes(1));
        return usage;
    }

    public async Task<bool> CheckLimitAsync(Guid tenantId, string metricKey)
    {
        var limit = await GetLimitAsync(tenantId, metricKey);
        if (limit == null) return true; // Unlimited

        var startOfPeriod = DateTime.UtcNow.AddDays(-30);
        var currentUsage = await GetUsageAsync(tenantId, metricKey, startOfPeriod);

        return currentUsage < limit.Value;
    }

    public async Task<decimal?> GetLimitAsync(Guid tenantId, string metricKey)
    {
        var cacheKey = $"limit:{tenantId}:{metricKey}";
        var cached = await _cache.GetAsync<decimal?>(cacheKey);
        if (cached != null) return cached;

        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == "active");
        if (sub == null) return null;

        var limitEntity = await _db.PlanLimits.FirstOrDefaultAsync(l => l.PlanId == sub.PlanId && l.MetricKey == metricKey);
        var limitVal = limitEntity?.MaxQuantity;

        if (limitVal.HasValue)
        {
            await _cache.SetAsync(cacheKey, limitVal.Value, TimeSpan.FromHours(1));
        }
        
        return limitVal;
    }
}
