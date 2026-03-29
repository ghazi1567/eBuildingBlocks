using System.Linq.Expressions;

namespace eBuildingBlocks.Infrastructure.Specifications;

/// <summary>
/// Optional EF Core–specific includes (expression and fluent Include/ThenInclude chains). Kept in Infrastructure so Domain stays free of EF types.
/// </summary>
public interface IEfSpecification<T> where T : class
{
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>Chained includes, e.g. <c>q => q.Include(x => x.Order).ThenInclude(o => o.Lines)</c>.</summary>
    IReadOnlyList<Func<IQueryable<T>, IQueryable<T>>> IncludeChains { get; }
}
