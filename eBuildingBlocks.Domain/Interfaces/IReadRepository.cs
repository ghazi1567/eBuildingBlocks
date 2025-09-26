namespace eBuildingBlocks.Domain.Interfaces
{
    public interface IReadRepository<TEntity, TKey> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
        Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
        Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
        Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken ct = default);
        Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
        Task<bool> AnyAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    }
}
