using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Domain.Models;
using eBuildingBlocks.Infrastructure.Data;
using eBuildingBlocks.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eBuildingBlocks.Infrastructure.Implementations;

public class Repository<TEntity, TKey, TDbContext>(
     TDbContext dbContext
    ) : UnitOfWork<TDbContext>(dbContext), IRepository<TEntity, TKey>, IEfQueryableRepository<TEntity, TKey>
    where TEntity : class, IEntity where TDbContext : DbContext
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

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Entities<TEntity>().Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Entities<TEntity>().Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        // FindAsync ignores global query filters; use a filtered query for tenant-scoped entities.
        if (typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)))
        {
            return await Entities<TEntity>()
                .FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id")!.Equals(id), ct);
        }

        return await Entities<TEntity>().FindAsync([id!], ct);
    }


    public virtual IQueryable<TEntity> Query() => SetAsNoTracking;

    public async Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec, dbContext).FirstOrDefaultAsync(ct);

    public async Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
       => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec, dbContext).SingleOrDefaultAsync(ct);


    public async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
      => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec, dbContext).ToListAsync(ct);


    public async Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken ct = default)
        => await Entities<TEntity>().AsNoTracking().ToListAsync(ct);

    public async Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
       => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec, dbContext).CountAsync(ct);


    public async Task<bool> AnyAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
     => await SpecificationEvaluator.GetQuery(Entities<TEntity>().AsQueryable(), spec, dbContext).AnyAsync(ct);

}