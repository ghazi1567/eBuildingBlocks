using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eBuildingBlocks.Infrastructure.Implementations;

public class Repository<TEntity, TKey, TDbContext>(
     TDbContext dbContext
    ) : UnitOfWork<TDbContext>(dbContext), IRepository<TEntity, TKey> where TEntity : class, Entity<TKey> where TDbContext : DbContext
{

    public IQueryable<TEntity> Queryable => Entities<TEntity>().AsQueryable();
    public virtual IQueryable<TEntity> GetQueryable => Queryable;
    public virtual IQueryable<TEntity> GetTrackedQueryable => Queryable;
    public virtual IQueryable<TEntity> ListQueryable => Queryable;
    public virtual IQueryable<TEntity> BulkUpdateQueryable => Queryable;
    public virtual IQueryable<TEntity> BulkDeleteQueryable => Queryable;
    public virtual Expression<Func<TEntity, TEntity>> ProjectionExpression => entity => entity;
    public virtual Expression<Func<TEntity, TEntity>> GetProjectionExpression => ProjectionExpression;
    public virtual Expression<Func<TEntity, TEntity>> ListProjectionExpression => ProjectionExpression;

    protected IQueryable<TEntity> SetAsNoTracking
    {
        get
        {
            var query = Entities<TEntity>().AsNoTracking();

            if (typeof(TEntity).IsSubclassOf(typeof(BaseEntity)))
            {
                query = query.Where(e => !(e as BaseEntity)!.IsDeleted);
            }

            return query;
        }
    }
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Entities<TEntity>().AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {

        await Task.Run(() =>
        {
            Entities<TEntity>().Update(entity);
        }, cancellationToken);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // TODO: enable this for soft delete. soft deleted record clean up pending.
        //if (entity is BaseEntity baseEntity)
        //{
        //    await Task.Run(() =>
        //    {
        //        Entities<TEntity>().Update(entity);
        //    }, cancellationToken);
        //}
        

        await Task.Run(() =>
        {
            Entities<TEntity>().Remove(entity);
        }, cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await Entities<TEntity>().SingleOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
    }
    public async Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable.Where(wherePredicate);
        return await query.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable.Where(wherePredicate);
        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellation = default)
    {
        var query = Entities<TEntity>().Where(wherePredicate);
        return await query.AsNoTracking().AnyAsync(cancellation);
    }
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await SetAsNoTracking.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var query = Entities<TEntity>();
        return await query.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public IQueryable<TEntity> Query()
    {
        return SetAsNoTracking;
    }

    public async Task<bool> CommitChangesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
        return true;
    }
}