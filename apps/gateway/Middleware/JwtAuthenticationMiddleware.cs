using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SaaS.Gateway.Middleware;

public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly HashSet<string> _publicPaths;

    public JwtAuthenticationMiddleware(
        RequestDelegate next, 
        ILogger<JwtAuthenticationMiddleware> logger,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        _issuer = config["Jwt:Issuer"] ?? "SaaS.Identity";
        _audience = config["Jwt:Audience"] ?? "SaaS.Platform";
        
        _publicPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/api/identity/auth/register",
            "/api/identity/auth/login",
            "/api/identity/auth/refresh",
            "/health",
            "/api/billing/webhook"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        
        if (_publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid authorization header" });
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            var tenantId = principal.FindFirst("tenant_id")?.Value;
            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(tenantId))
            {
                context.Request.Headers["X-Tenant-Id"] = tenantId;
            }
            if (!string.IsNullOrEmpty(userId))
            {
                context.Request.Headers["X-User-Id"] = userId;
            }
            if (!string.IsNullOrEmpty(role))
            {
                context.Request.Headers["X-User-Role"] = role;
            }

            context.User = principal;

            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token expired for request to {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Token expired" });
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid token for request to {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token for request to {Path}", path);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication error" });
        }
    }
}
