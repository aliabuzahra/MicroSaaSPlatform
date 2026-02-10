using StackExchange.Redis;
using System.Net;

namespace SaaS.Gateway.Middleware;

public class RedisRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimitingMiddleware> _logger;
    private readonly int _limit;
    private readonly int _windowSeconds;

    public RedisRateLimitingMiddleware(RequestDelegate next, IConnectionMultiplexer redis, ILogger<RedisRateLimitingMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
        _limit = config.GetValue<int>("RateLimiting:PermitLimit", 100);
        _windowSeconds = config.GetValue<int>("RateLimiting:Window", 60);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var identifier = context.Request.Headers["X-Tenant-Id"].FirstOrDefault() 
                         ?? context.Connection.RemoteIpAddress?.ToString() 
                         ?? "unknown";

        var currentMinute = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        var key = $"ratelimit:{identifier}:{currentMinute}";
        var db = _redis.GetDatabase();

        var count = await db.StringIncrementAsync(key);
        
        if (count == 1)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(_windowSeconds));
        }

        if (count > _limit)
        {
            _logger.LogWarning("Rate limit exceeded for {Identifier}", identifier);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            return;
        }

        await _next(context);
    }
}
