using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Domain.Models;
using eBuildingBlocks.Domain.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eBuildingBlocks.Infrastructure.Implementations;

public class Repository<TEntity, TKey, TDbContext>(
     TDbContext dbContext
    ) : UnitOfWork<TDbContext>(dbContext), IRepository<TEntity, TKey> where TEntity : class, IEntity where TDbContext : DbContext
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
            return Entities<TEntity>().AsNoTracking();
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

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
         => await Entities<TEntity>().FindAsync(new[] { id }, ct);


    public IQueryable<TEntity> Query()
    {
        return SetAsNoTracking;
    }

   

    public async Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec).FirstOrDefaultAsync(ct);

    public async Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
       => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec).SingleOrDefaultAsync(ct);


    public async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
      => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec).ToListAsync(ct);


    public async Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken ct = default)
        => await Entities<TEntity>().AsNoTracking().ToListAsync(ct);

    public async Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
       => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec).CountAsync(ct);


    public async Task<bool> AnyAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
     => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec).AnyAsync(ct);

}