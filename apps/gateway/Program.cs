using SaaS.Shared.Kernel.Extensions;
using SaaS.Gateway.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));
builder.Services.AddRedisCache(redisConn);

var app = builder.Build();

app.UseCors();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseMiddleware<RedisRateLimitingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Gateway" }));

app.MapReverseProxy();

app.Run();

public partial class Program { }
