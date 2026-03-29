using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Specifications;

/// <summary>
/// Inlines <see cref="Expression{TDelegate}"/> bodies into a shared parameter so composite criteria avoid
/// <see cref="Expression.Invoke"/>, which EF Core typically cannot translate to SQL.
/// </summary>
internal static class PredicateExpressionInliner
{
    /// <summary>
    /// Returns <paramref name="predicate"/>'s body with its parameter replaced by <paramref name="targetParameter"/>.
    /// </summary>
    public static Expression InlineBody<T>(Expression<Func<T, bool>> predicate, ParameterExpression targetParameter)
    {
        if (predicate.Parameters.Count != 1)
            throw new ArgumentException("Predicate must have exactly one parameter.", nameof(predicate));

        return new ParameterReplacer(predicate.Parameters[0], targetParameter).Visit(predicate.Body)
               ?? throw new InvalidOperationException("Predicate body rewrite produced null.");
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterReplacer(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
