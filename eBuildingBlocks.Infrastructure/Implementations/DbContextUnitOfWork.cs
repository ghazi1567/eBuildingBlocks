using eBuildingBlocks.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBuildingBlocks.Infrastructure.Implementations;

/// <summary>
/// Adapts <see cref="DbContext.SaveChangesAsync(System.Threading.CancellationToken)"/> to <see cref="IUnitOfWork"/>.
/// Register as scoped alongside your <typeparamref name="TDbContext"/>.
/// </summary>
public sealed class DbContextUnitOfWork<TDbContext>(TDbContext context) : IUnitOfWork where TDbContext : DbContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
