using eBuildingBlocks.Domain.Interfaces;
using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Specifications
{
    public abstract class Specification<T> : ISpecification<T>
    {
        public Expression<Func<T, bool>>? Criteria { get; protected set; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public List<string> IncludeStrings { get; } = new();
        public Expression<Func<T, object>>? OrderBy { get; protected set; }
        public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
        public int? Take { get; protected set; }
        public int? Skip { get; protected set; }
        public bool AsNoTracking { get; protected set; } = true;
        public bool IgnoreQueryFilters { get; protected set; } = false;

        protected void AddInclude(Expression<Func<T, object>> include) => Includes.Add(include);
        protected void AddInclude(string includeString) => IncludeStrings.Add(includeString);
        protected void ApplyPaging(int skip, int take) { Skip = skip; Take = take; }
        protected void ApplyOrderBy(Expression<Func<T, object>> orderBy) => OrderBy = orderBy;

        /// <summary>
        /// Pass a string like "Name" or "Category.Name" to sort by a property or a nested property.
        /// </summary>
        /// <param name="sortBy"></param>
        protected void ApplyOrderBy(string sortBy)
        {
            var keySelector = BuildOrderByExpression(sortBy);
            ApplyOrderBy(keySelector);
        }
        protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc) => OrderByDescending = orderByDesc;
        /// <summary>
        /// Pass a string like "Name" or "Category.Name" to sort by a property or a nested property.
        /// </summary>
        /// <param name="sortBy"></param>
        protected void ApplyOrderByDescending(string sortBy)
        {
            var keySelector = BuildOrderByExpression(sortBy);
            ApplyOrderByDescending(keySelector);
        }

        protected void WithTracking() => AsNoTracking = false;
        protected void WithIgnoreQueryFilters() => IgnoreQueryFilters = true;
        private static Expression<Func<T, object>> BuildOrderByExpression(string sortBy)
        {
            // supports "category.name"
            var param = Expression.Parameter(typeof(T), "p");
            Expression body = param;

            foreach (var member in sortBy.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                body = Expression.PropertyOrField(body, member);

            // box to object
            var converted = Expression.Convert(body, typeof(object));
            return (Expression.Lambda<Func<T, object>>(converted, param));


        }

        public void CompositeFilter(IEnumerable<FilterCriterion> filters, Logical logical = Logical.And)
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
