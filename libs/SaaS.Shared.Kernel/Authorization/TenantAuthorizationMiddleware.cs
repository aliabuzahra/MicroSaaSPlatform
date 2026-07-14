using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SaaS.Shared.Kernel.BuildingBlocks;

namespace SaaS.Shared.Kernel.Authorization;

public class TenantAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantAuthorizationMiddleware> _logger;

    public TenantAuthorizationMiddleware(RequestDelegate next, ILogger<TenantAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
            
            if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                context.Request.Headers["X-Tenant-Id"] = tenantId.ToString();
                _logger.LogDebug("Set tenant context from JWT: {TenantId}", tenantId);
            }
        }

        await _next(context);
    }
}

public static class TenantAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantAuthorizationMiddleware>();
    }
}

public static class HttpContextExtensions
{
    public static Guid? GetCurrentUserId(this HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? context.User.FindFirst("sub")?.Value;
        
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        
        return null;
    }

    public static Guid? GetCurrentTenantId(this HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
            return tenantId;
        
        var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (Guid.TryParse(headerTenantId, out tenantId))
            return tenantId;
        
        return null;
    }

    public static string? GetCurrentUserRole(this HttpContext context)
    {
        return context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
    }

    public static string? GetCurrentUserEmail(this HttpContext context)
    {
        return context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
               ?? context.User.FindFirst("email")?.Value;
    }
}
