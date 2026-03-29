using eBuildingBlocks.Domain.Interfaces;
using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Specifications;

/// <summary>
/// Base for domain specifications without EF-specific include expressions.
/// </summary>
public abstract class SpecificationBase<T> : ISpecification<T> where T : class
{
    private readonly List<string> _includeStrings = new();

    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    public IReadOnlyList<string> IncludeStrings => _includeStrings;

    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
    public int? Take { get; protected set; }
    public int? Skip { get; protected set; }
    public bool AsNoTracking { get; protected set; } = true;
    public bool IgnoreQueryFilters { get; protected set; }

    protected void AddInclude(string includeString) => _includeStrings.Add(includeString);

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;

    /// <summary>Sort by a property path such as <c>Name</c> or <c>Category.Name</c>.</summary>
    protected void ApplyOrderBy(string sortBy)
    {
        var keySelector = BuildOrderByExpression(sortBy);
        ApplyOrderBy(keySelector);
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc) => OrderByDescending = orderByDesc;

    /// <summary>Sort descending by a property path such as <c>Name</c> or <c>Category.Name</c>.</summary>
    protected void ApplyOrderByDescending(string sortBy)
    {
        var keySelector = BuildOrderByExpression(sortBy);
        ApplyOrderByDescending(keySelector);
    }

    protected void WithTracking() => AsNoTracking = false;

    protected void WithIgnoreQueryFilters() => IgnoreQueryFilters = true;

    private static Expression<Func<T, object>> BuildOrderByExpression(string sortBy)
    {
        var param = Expression.Parameter(typeof(T), "p");
        Expression body = param;

        foreach (var member in sortBy.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            body = Expression.PropertyOrField(body, member);

        var converted = Expression.Convert(body, typeof(object));
        return Expression.Lambda<Func<T, object>>(converted, param);
    }

    public void CompositeFilter(IEnumerable<FilterCriterion> filters, Logical logical = Logical.And)
    {
        var param = Expression.Parameter(typeof(T), "e");
        Expression? body = null;

        foreach (var f in filters)
        {
            var pred = DynamicPredicate.Build<T>(f.Field, f.Op, f.Value);
            var part = PredicateExpressionInliner.InlineBody(pred, param);

            body = body is null
                ? part
                : (logical == Logical.And ? Expression.AndAlso(body, part) : Expression.OrElse(body, part));
        }

        if (body is null) body = Expression.Constant(true);
        Criteria = Expression.Lambda<Func<T, bool>>(body, param);
    }
}
