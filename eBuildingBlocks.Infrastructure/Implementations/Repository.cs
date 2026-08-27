using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Domain.Models;
using eBuildingBlocks.Infrastructure.Data;
using eBuildingBlocks.Infrastructure.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eBuildingBlocks.Infrastructure.Implementations;

/// <summary>
/// EF Core-backed repository. Composes <typeparamref name="TDbContext"/> directly rather than
/// inheriting <see cref="UnitOfWork{TDbContext}"/> — a repository is not a unit of work, and IS-A
/// inheritance previously put <c>SaveChangesAsync</c>/<c>BeginTransactionAsync</c>/<c>ExecuteSqlAsync</c>
/// on the repository's public surface. Persist changes via <see cref="IUnitOfWork"/> instead
/// (e.g. <see cref="DbContextUnitOfWork{TDbContext}"/>, registered separately via <c>AddDbContextUnitOfWork</c>).
/// </summary>
public class Repository<TEntity, TKey, TDbContext>(
     TDbContext dbContext
    ) : IRepository<TEntity, TKey>, IEfQueryableRepository<TEntity, TKey>
    where TEntity : class, IEntity where TDbContext : DbContext
{
    private DbSet<TEntity> Entities() => dbContext.Set<TEntity>();

    public IQueryable<TEntity> Queryable => Entities().AsQueryable();
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
            return Entities().AsNoTracking();
        }
    }
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Entities().AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Entities().Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Entities().Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        // FindAsync ignores global query filters; use a filtered query for tenant-scoped entities.
        if (typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)))
        {
            return await Entities()
                .FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id")!.Equals(id), ct);
        }

        return await Entities().FindAsync([id!], ct);
    }


    public virtual IQueryable<TEntity> Query() => SetAsNoTracking;

    public async Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
        => await SpecificationEvaluator.GetQuery(Entities().AsQueryable(), spec, dbContext).FirstOrDefaultAsync(ct);

    public async Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
       => await SpecificationEvaluator.GetQuery(Entities().AsQueryable(), spec, dbContext).SingleOrDefaultAsync(ct);


    public async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
      => await SpecificationEvaluator.GetQuery(Entities().AsQueryable(), spec, dbContext).ToListAsync(ct);


    public async Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken ct = default)
        => await Entities().AsNoTracking().ToListAsync(ct);

    public async Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
       => await SpecificationEvaluator.GetQuery(Entities().AsQueryable(), spec, dbContext).CountAsync(ct);


    public async Task<bool> AnyAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
     => await SpecificationEvaluator.GetQuery(Entities().AsQueryable(), spec, dbContext).AnyAsync(ct);

}
