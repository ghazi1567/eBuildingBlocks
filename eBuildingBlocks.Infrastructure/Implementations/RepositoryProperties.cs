using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace eBuildingBlocks.Infrastructure.Implementations;

public class RepositoryProperties<TEntity, TKey>(
    DbContext dbContext
    ) where TEntity : class, Entity<TKey>
{
    protected readonly DbContext _dbContext = dbContext;

    protected DbSet<TEntity> Set => _dbContext.Set<TEntity>();

    protected IQueryable<TEntity> SetAsNoTracking
    {
        get
        {
            var query = Set.AsNoTracking();

            if (typeof(TEntity).IsSubclassOf(typeof(BaseEntity)))
            {
                query = query.Where(e => !(e as BaseEntity)!.IsDeleted);
            }

            return query;
        }
    }
}
