using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Interfaces;

/// <summary>
/// Persistence-agnostic query specification (criteria, string includes, ordering, paging).
/// Expression-based EF <c>Include</c> / <c>ThenInclude</c> chains belong in <c>IEfSpecification&lt;T&gt;</c> (Infrastructure).
/// </summary>
public interface ISpecification<T> where T : class
{
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>Dot-separated navigation paths, e.g. <c>Category</c> or <c>Order.Lines</c>.</summary>
    IReadOnlyList<string> IncludeStrings { get; }

    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }

    int? Take { get; }
    int? Skip { get; }
    bool AsNoTracking { get; }

    /// <summary>When true, the provider should ignore global query filters (e.g. tenant filter).</summary>
    bool IgnoreQueryFilters { get; }
}
