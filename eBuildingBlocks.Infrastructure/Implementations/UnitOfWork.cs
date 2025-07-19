using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace eBuildingBlocks.Infrastructure.Implementations;

public class UnitOfWork<TDbContext>(TDbContext dbContext) where TDbContext : DbContext
{
    public DbSet<TEntity> Entities<TEntity>()
           where TEntity : class, IEntity
    {
        return dbContext.Set<TEntity>();
    }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
    public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        return dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public async Task<bool> ExecuteSqlAsync(FormattableString sql, CancellationToken cancellationToken)
    {
        return await dbContext.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken) > 0;
    }

}
