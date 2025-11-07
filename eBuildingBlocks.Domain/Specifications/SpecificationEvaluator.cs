using eBuildingBlocks.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Specifications
{
    public static class SpecificationEvaluator
    {
        public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> spec)
            where T : class
        {
            var query = inputQuery;

            if (spec.IgnoreQueryFilters)
                query = query.IgnoreQueryFilters();

            if (spec.Criteria is not null)
                query = query.Where(spec.Criteria);

            // includes
            query = spec.Includes.Aggregate(query, (q, include) => q.Include(include));
            query = spec.IncludeStrings.Aggregate(query, (q, include) => q.Include(include));
            // apply Include + ThenInclude style
            query = spec.IncludeExpressions.Aggregate(query, (current, include) => include(current));

            // ordering
            if (spec.OrderBy is not null) query = query.OrderBy(spec.OrderBy);
            if (spec.OrderByDescending is not null) query = query.OrderByDescending(spec.OrderByDescending);

            // paging
            if (spec.Skip.HasValue) query = query.Skip(spec.Skip.Value);
            if (spec.Take.HasValue) query = query.Take(spec.Take.Value);

            // tracking
            if (spec.AsNoTracking) query = query.AsNoTracking();

            return query;
        }
    }
}
