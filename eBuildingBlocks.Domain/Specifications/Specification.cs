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
        protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc) => OrderByDescending = orderByDesc;
        protected void WithTracking() => AsNoTracking = false;
        protected void WithIgnoreQueryFilters() => IgnoreQueryFilters = true;
    }
}
