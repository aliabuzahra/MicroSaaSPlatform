using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using System.Text.Json;

namespace SaaS.Shared.Kernel.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddServiceHealthChecks(
        this IServiceCollection services,
        string? postgresConnectionString = null,
        string? rabbitMqConnectionString = null,
        string? redisConnectionString = null)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            healthChecksBuilder.AddNpgSql(
                postgresConnectionString,
                name: "postgresql",
                tags: ["db", "ready"]);
        }

        if (!string.IsNullOrEmpty(rabbitMqConnectionString))
        {
            var rabbitUri = rabbitMqConnectionString;
            healthChecksBuilder.AddRabbitMQ(
                sp => 
                {
                    var factory = new ConnectionFactory { Uri = new Uri(rabbitUri) };
                    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                },
                name: "rabbitmq",
                tags: ["messaging", "ready"]);
        }

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                tags: ["cache", "ready"]);
        }

        return services;
    }

    public static IApplicationBuilder UseServiceHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteHealthCheckResponse,
            AllowCachingResponses = false
        });

        return app;
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message,
                tags = e.Value.Tags
            })
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
