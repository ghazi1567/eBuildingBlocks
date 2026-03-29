using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Specifications
{
    public enum Logical
    {
        And,
        Or
    }

    public sealed record FilterCriterion(string Field, ComparisonOperator Op, object? Value);

    public sealed class CompositeFilterSpec<T> : SpecificationBase<T> where T : class
    {
        public CompositeFilterSpec(IEnumerable<FilterCriterion> filters, Logical logical = Logical.And)
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

            if (body is null) body = Expression.Constant(true); // no filters → always true
            Criteria = Expression.Lambda<Func<T, bool>>(body, param);
        }
    }

}
