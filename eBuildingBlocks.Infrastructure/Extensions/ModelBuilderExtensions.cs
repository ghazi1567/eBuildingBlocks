using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

// Namespace updated to eBuildingBlocks
namespace eBuildingBlocks.Infrastructure.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void ApplyGlobalTenantFilter(this ModelBuilder modelBuilder, Guid tenantId, Type baseType = null)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (baseType != null && !baseType.IsAssignableFrom(entityType.ClrType))
                    continue;

                var method = typeof(ModelBuilderExtensions)
                    .GetMethod(nameof(SetGlobalQuery), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { modelBuilder, tenantId });
            }
        }

        private static void SetGlobalQuery<TEntity>(ModelBuilder modelBuilder, Guid tenantId) where TEntity : class, ITenantEntity
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == tenantId);
        }
    }
}
