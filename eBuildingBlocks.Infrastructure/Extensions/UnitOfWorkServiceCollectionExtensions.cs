using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eBuildingBlocks.Infrastructure.Extensions;

/// <summary>
/// Registers <see cref="IUnitOfWork"/> for EF Core applications.
/// </summary>
public static class UnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="DbContextUnitOfWork{TDbContext}"/> as scoped <see cref="IUnitOfWork"/>.
    /// </summary>
    public static IServiceCollection AddDbContextUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IUnitOfWork, DbContextUnitOfWork<TDbContext>>();
        return services;
    }
}
