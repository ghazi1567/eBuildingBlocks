using eBuildingBlocks.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace eBuildingBlocks.Infrastructure.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies <c>HasQueryFilter</c> so each <see cref="ITenantEntity"/> row is restricted to <see cref="ICurrentUser.TenantId"/>
    /// (evaluated when the query runs). Call only when multi-tenancy is enabled.
    /// </summary>
    public static void ApplyRuntimeTenantQueryFilters(this ModelBuilder modelBuilder, ICurrentUser currentUser)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(et => typeof(ITenantEntity).IsAssignableFrom(et.ClrType)))
        {
            var method = typeof(ModelBuilderExtensions).GetMethod(
                             nameof(SetTenantFilter),
                             BindingFlags.NonPublic | BindingFlags.Static)!
                         .MakeGenericMethod(entityType.ClrType);
            method.Invoke(null, [modelBuilder, currentUser]);
        }
    }

    private static void SetTenantFilter<TEntity>(ModelBuilder modelBuilder, ICurrentUser currentUser)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == currentUser.TenantId);
    }

    /// <summary>
    /// Adds an index on <see cref="ITenantEntity.TenantId"/> for all tenant-scoped entity types.
    /// </summary>
    public static void ApplyTenantEntityIndexes(this ModelBuilder modelBuilder)
    {
        foreach (var et in modelBuilder.Model.GetEntityTypes()
                     .Where(et => typeof(ITenantEntity).IsAssignableFrom(et.ClrType)))
        {
            modelBuilder.Entity(et.ClrType).HasIndex(nameof(ITenantEntity.TenantId));
        }
    }

    /// <summary>
    /// Obsolete: used a compile-time constant tenant id. Use <see cref="ApplyRuntimeTenantQueryFilters"/> with <see cref="ICurrentUser"/> instead.
    /// </summary>
    [Obsolete("Use ApplyRuntimeTenantQueryFilters(ModelBuilder, ICurrentUser) on TenantAwareDbContext instead.")]
    public static void ApplyGlobalTenantFilter(this ModelBuilder modelBuilder, Guid tenantId)
    {
        foreach (var et in modelBuilder.Model.GetEntityTypes()
                     .Where(et => typeof(ITenantEntity).IsAssignableFrom(et.ClrType)))
        {
            var param = Expression.Parameter(et.ClrType, "e");
            var prop = Expression.Property(param, nameof(ITenantEntity.TenantId));
            var currentTenant = Expression.Constant(tenantId, typeof(Guid));
            var body = Expression.Equal(prop, currentTenant);
            var lambda = Expression.Lambda(body, param);
            modelBuilder.Entity(et.ClrType).HasQueryFilter(lambda);
        }

        foreach (var et in modelBuilder.Model.GetEntityTypes()
                     .Where(et => typeof(ITenantEntity).IsAssignableFrom(et.ClrType)))
        {
            modelBuilder.Entity(et.ClrType).HasIndex(nameof(ITenantEntity.TenantId));
        }
    }
}
