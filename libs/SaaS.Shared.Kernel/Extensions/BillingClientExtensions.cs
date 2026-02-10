using Microsoft.Extensions.DependencyInjection;
using SaaS.Shared.Kernel.Clients;

namespace SaaS.Shared.Kernel.Extensions;

public static class BillingClientExtensions
{
    public static IServiceCollection AddBillingClient(this IServiceCollection services, Action<HttpClient> configureClient)
    {
        services.AddHttpClient<IBillingClient, BillingClient>(configureClient);
        return services;
    }
}
