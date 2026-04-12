using eBuildingBlocks.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace eBuildingBlocks.Infrastructure.Specifications;

public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> input, ISpecification<T> spec, DbContext dbContext)
        where T : class
    {
        var query = input;

        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        if (spec.IgnoreQueryFilters)
        {
            var bypass = dbContext.GetService<IQueryFilterBypassEvaluator>();
            if (bypass?.CanIgnoreGlobalQueryFilters != true)
            {
                throw new InvalidOperationException(
                    "IgnoreQueryFilters is not allowed unless IQueryFilterBypassEvaluator.CanIgnoreGlobalQueryFilters is true (register an elevated evaluator for admin/migration scenarios).");
            }

            query = query.IgnoreQueryFilters();
        }

        if (spec is IEfSpecification<T> ef)
        {
            foreach (var include in ef.Includes)
                query = query.Include(include);

            foreach (var chain in ef.IncludeChains)
                query = chain(query);
        }

        foreach (var includeString in spec.IncludeStrings)
            query = query.Include(includeString);

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.Skip is { } skip)
            query = query.Skip(skip);

        if (spec.Take is { } take)
            query = query.Take(take);

        query = spec.AsNoTracking ? query.AsNoTracking() : query;

        return query;
    }
}
