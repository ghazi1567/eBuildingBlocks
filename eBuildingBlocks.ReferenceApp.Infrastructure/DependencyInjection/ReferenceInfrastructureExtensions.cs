using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Extensions;
using eBuildingBlocks.Infrastructure.Implementations;
using eBuildingBlocks.Infrastructure.Outbox;
using eBuildingBlocks.ReferenceApp.Domain.Orders;
using eBuildingBlocks.ReferenceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eBuildingBlocks.ReferenceApp.Infrastructure.DependencyInjection;

public static class ReferenceInfrastructureExtensions
{
    /// <summary>EF Core, repositories, unit of work, and outbox interceptor wiring for the reference host.</summary>
    public static IServiceCollection AddReferenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ReferenceDatabase")
            ?? throw new InvalidOperationException("Connection string 'ReferenceDatabase' is required.");

        // DomainOutboxSaveChangesInterceptor is registered by AddTransactionalOutboxInfrastructure<TDbContext>() in the host.

        services.AddDbContext<ReferenceDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);

            // Ordering: audit first so stamps exist before outbox serialization reads aggregates (either order works for this app).
            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<DomainOutboxSaveChangesInterceptor>());
        });

        services.AddScoped<AuditSaveChangesInterceptor>();

        // Closed registration: Repository<TEntity,TKey,TDbContext> has three arity; IRepository<,> has two.
        services.AddScoped<IRepository<Order, Guid>, Repository<Order, Guid, ReferenceDbContext>>();

        services.AddDbContextUnitOfWork<ReferenceDbContext>();

        return services;
    }
}
