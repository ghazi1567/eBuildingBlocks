using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Data;

namespace eBuildingBlocks.Infrastructure.Extensions;

public static class EfRepositoryQueryableExtensions
{
    /// <summary>
    /// Casts a domain repository to the EF query port when the implementation supports it (e.g. <c>Repository&lt;,,&gt;</c>).
    /// </summary>
    public static IEfQueryableRepository<TEntity, TKey>? AsEfQueryable<TEntity, TKey>(this IRepository<TEntity, TKey> repository)
        where TEntity : class
        => repository as IEfQueryableRepository<TEntity, TKey>;
}
