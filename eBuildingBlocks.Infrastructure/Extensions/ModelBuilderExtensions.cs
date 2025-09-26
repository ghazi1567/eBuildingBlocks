using eBuildingBlocks.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

// Namespace updated to eBuildingBlocks
namespace eBuildingBlocks.Infrastructure.Extensions
{
    public static class ModelBuilderExtensions
    {
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


            // (Optional) index TenantId everywhere it exists
            foreach (var et in modelBuilder.Model.GetEntityTypes()
                     .Where(et => typeof(ITenantEntity).IsAssignableFrom(et.ClrType)))
            {
                modelBuilder.Entity(et.ClrType).HasIndex(nameof(ITenantEntity.TenantId));
            }
        }

       
    }
}
