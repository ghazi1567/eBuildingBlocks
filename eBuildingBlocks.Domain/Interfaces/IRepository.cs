using eBuildingBlocks.Domain.Models;
using eBuildingBlocks.Domain.SpecificationConfig;
using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Interfaces;

public interface IRepository<TEntity, TKey> where TEntity : class, Entity<TKey>
{
    Task<bool> CommitChangesAsync(CancellationToken cancellationToken = default);
    IQueryable<TEntity> Query();
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
     Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);


    Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellationToken = default);

   
}
