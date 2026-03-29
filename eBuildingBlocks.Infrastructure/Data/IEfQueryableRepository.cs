namespace eBuildingBlocks.Infrastructure.Data;

/// <summary>
/// EF Core query surface; not part of domain <c>IRepository&lt;,&gt;</c> to avoid leaking <see cref="IQueryable{T}"/> across the domain boundary.
/// Resolve <see cref="Implementations.Repository{TEntity,TKey,TDbContext}"/> as this interface (or cast via <c>AsEfQueryable</c>) when LINQ-to-EF is required.
/// </summary>
public interface IEfQueryableRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>Default is typically AsNoTracking; override in a derived repository if you need a different policy.</summary>
    IQueryable<TEntity> Query();
}
