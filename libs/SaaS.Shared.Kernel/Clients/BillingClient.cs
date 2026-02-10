namespace SaaS.Shared.Kernel.Clients;

using System.Net.Http.Json;

public interface IBillingClient
{
    Task<bool> CheckLimitAsync(Guid tenantId, string metricKey);
    Task TrackUsageAsync(Guid tenantId, string metricKey, decimal quantity);
}

public class BillingClient : IBillingClient
{
    private readonly HttpClient _httpClient;

    public BillingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckLimitAsync(Guid tenantId, string metricKey)
    {
        // Call GET /api/billing/usage?tenantId=...&metricKey=...
        // Expect { "usage": X, "isAllocated": bool }
        var response = await _httpClient.GetFromJsonAsync<UsageResponse>($"/api/billing/usage?tenantId={tenantId}&metricKey={metricKey}");
        return response?.IsAllocated ?? false;
    }

    public async Task TrackUsageAsync(Guid tenantId, string metricKey, decimal quantity)
    {
        await _httpClient.PostAsJsonAsync("/api/billing/usage/events", new { TenantId = tenantId, MetricKey = metricKey, Quantity = quantity });
    }

    private class UsageResponse
    {
        public decimal Usage { get; set; }
        public bool IsAllocated { get; set; }
    }
}
