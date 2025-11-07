using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace eBuildingBlocks.Domain.Interfaces
{
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>>? Criteria { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        List<string> IncludeStrings { get; }
        List<Func<IQueryable<T>, IIncludableQueryable<T, object>>> IncludeExpressions { get; }
        Expression<Func<T, object>>? OrderBy { get; }
        Expression<Func<T, object>>? OrderByDescending { get; }

        int? Take { get; }
        int? Skip { get; }
        bool AsNoTracking { get; }

        /// <summary>When true, EF will ignore global query filters (e.g., tenant filter).</summary>
        bool IgnoreQueryFilters { get; }
    }
}
