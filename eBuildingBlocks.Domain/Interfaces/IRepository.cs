namespace eBuildingBlocks.Domain.Interfaces;

public interface IRepository<TEntity, TKey> : IReadRepository<TEntity, TKey> where TEntity : class
{
    Task<bool> CommitChangesAsync(CancellationToken cancellationToken = default);
    IQueryable<TEntity> Query();
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

}
