using SaaS.Shared.Kernel.Extensions;
using SaaS.Gateway.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddRedisCache(redisConn);

var app = builder.Build();

app.UseMiddleware<RedisRateLimitingMiddleware>();

app.MapReverseProxy();

app.Run();
