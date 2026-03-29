namespace eBuildingBlocks.Domain.Interfaces;

/// <summary>
/// Persistence port for a single aggregate root / entity set.
/// </summary>
/// <remarks>
/// <see cref="IQueryable{T}"/> is not on this interface; EF Core hosts expose
/// <c>eBuildingBlocks.Infrastructure.Data.IEfQueryableRepository&lt;TEntity,TKey&gt;</c> (or cast with <c>AsEfQueryable</c>).
/// Prefer <see cref="ISpecification{TEntity}"/> or explicit repository methods for persistence-agnostic reads.
/// Persist changes via <see cref="IUnitOfWork.SaveChangesAsync"/> (typically your <c>DbContext</c> or repository's unit-of-work base).
/// </remarks>
public interface IRepository<TEntity, TKey> : IReadRepository<TEntity, TKey> where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
