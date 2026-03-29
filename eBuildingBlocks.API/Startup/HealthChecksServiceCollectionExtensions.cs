using eBuildingBlocks.Common.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.API.Startup;

/// <summary>ASP.NET Core health checks aligned with <c>Features:Endpoints:HealthChecks</c> (used by <see cref="AppUseExtensions.UsingEndpoints"/>).</summary>
public static class HealthChecksServiceCollectionExtensions
{
    /// <summary>
    /// Registers health checks and adds an EF Core <see cref="DbContext"/> check. No-ops when the feature is disabled.
    /// </summary>
    public static IServiceCollection RegisterHealthChecksWithDbContext<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string registrationName = "database")
        where TDbContext : DbContext
    {
        if (!FeatureGate.Enabled(configuration, "Features:Endpoints:HealthChecks")) return services;

        services.AddHealthChecks()
            .AddDbContextCheck<TDbContext>(registrationName);

        return services;
    }

    /// <summary>Registers health checks without an EF check (for custom <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck"/> registrations).</summary>
    public static IServiceCollection RegisterHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        if (!FeatureGate.Enabled(configuration, "Features:Endpoints:HealthChecks")) return services;

        services.AddHealthChecks();
        return services;
    }
}
