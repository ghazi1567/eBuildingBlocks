using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Caching;
using eBuildingBlocks.Infrastructure.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eBuildingBlocks.Infrastructure.Tenancy;

/// <summary>
/// Registers tenant resolution (<see cref="ICurrentUser"/>, <see cref="ITenantScope"/>), query-filter bypass policy, and tenant-scoped memory cache.
/// Use from API hosts, MassTransit workers, or Hangfire dashboards that resolve tenant from messages/jobs via <see cref="ITenantScope.Begin"/>.
/// </summary>
public static class TenantServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="MultiTenancyOptions"/> from <c>Features:MultiTenancy</c>, registers HTTP accessor, ambient tenant scope, and <see cref="ICurrentUser"/>.
    /// </summary>
    public static IServiceCollection AddTenantContextCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MultiTenancyOptions>(configuration.GetSection("Features:MultiTenancy"));
        services.AddHttpContextAccessor();
        services.AddSingleton<ITenantScope, TenantScope>();
        services.AddSingleton<ICurrentUser, TenantResolver>();
        services.AddSingleton<IQueryFilterBypassEvaluator, DenyQueryFilterBypassEvaluator>();
        return services;
    }

    /// <summary>Alias for worker / console hosts that do not use the full API registration pipeline.</summary>
    public static IServiceCollection AddTenantContextForWorkers(this IServiceCollection services, IConfiguration configuration)
        => AddTenantContextCore(services, configuration);

    /// <summary>
    /// Registers <see cref="ITenantMemoryCache"/> when <see cref="IMemoryCache"/> is already registered.
    /// </summary>
    public static IServiceCollection AddTenantMemoryCache(this IServiceCollection services)
    {
        services.AddSingleton<ITenantMemoryCache, TenantMemoryCache>();
        return services;
    }
}
