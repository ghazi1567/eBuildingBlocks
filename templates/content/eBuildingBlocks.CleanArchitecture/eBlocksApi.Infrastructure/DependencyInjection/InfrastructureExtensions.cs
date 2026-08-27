using eBlocksApi.Domain.Products;
using eBlocksApi.Infrastructure.Data;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Extensions;
using eBuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eBlocksApi.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    /// <summary>EF Core (in-memory by default), audit interceptor, repository, and unit of work wiring.</summary>
    public static IServiceCollection AddAppInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase("eBlocksApi");
            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddDbContextUnitOfWork<AppDbContext>();

        // Repository<TEntity,TKey,TDbContext> has three type parameters but IRepository<TEntity,TKey> only two,
        // so register per-entity rather than as an open generic.
        services.AddScoped<IRepository<Product, Guid>, Repository<Product, Guid, AppDbContext>>();

        return services;
    }
}
