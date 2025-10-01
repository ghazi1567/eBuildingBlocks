using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Specifications
{
    public enum Logical
    {
        And,
        Or
    }

    public sealed record FilterCriterion(string Field, ComparisonOperator Op, object? Value);

    public sealed class CompositeFilterSpec<T> : Specification<T>
    {
        public CompositeFilterSpec(IEnumerable<FilterCriterion> filters, Logical logical = Logical.And)
        {
            var param = Expression.Parameter(typeof(T), "e");
            Expression? body = null;

            foreach (var f in filters)
            {
                var pred = DynamicPredicate.Build<T>(f.Field, f.Op, f.Value);
                var invoked = Expression.Invoke(pred, param);

                body = body is null
                    ? invoked
                    : (logical == Logical.And ? Expression.AndAlso(body, invoked) : Expression.OrElse(body, invoked));
            }

            if (body is null) body = Expression.Constant(true); // no filters → always true
            Criteria = Expression.Lambda<Func<T, bool>>(body, param);
        }
    }

}
