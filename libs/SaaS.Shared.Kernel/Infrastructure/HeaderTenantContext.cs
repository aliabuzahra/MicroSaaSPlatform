using Microsoft.AspNetCore.Http;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Infrastructure;

public class HeaderTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public TenantId TenantId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return TenantId.Empty;
            }

            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
            {
                if (Guid.TryParse(tenantIdValues.FirstOrDefault(), out var tenantId))
                {
                    return new TenantId(tenantId);
                }
            }

            return TenantId.Empty;
        }
    }
}
