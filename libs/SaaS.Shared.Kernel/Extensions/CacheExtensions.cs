using Microsoft.Extensions.DependencyInjection;
using SaaS.Shared.Kernel.Infrastructure.Caching;

namespace SaaS.Shared.Kernel.Extensions;

public static class CacheExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, string connectionString)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "SaaS_";
        });

        services.AddScoped<ICacheService, RedisCacheService>();
        return services;
    }
}
