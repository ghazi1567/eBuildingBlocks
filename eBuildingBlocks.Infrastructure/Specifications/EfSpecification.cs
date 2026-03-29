using eBuildingBlocks.Domain.Specifications;
using System.Linq.Expressions;

namespace eBuildingBlocks.Infrastructure.Specifications;

/// <summary>
/// Specification base that adds EF Core include expressions; criteria and string includes remain on <see cref="SpecificationBase{T}"/>.
/// </summary>
public abstract class EfSpecification<T> : SpecificationBase<T>, IEfSpecification<T> where T : class
{
    private readonly List<Expression<Func<T, object>>> _includes = new();
    private readonly List<Func<IQueryable<T>, IQueryable<T>>> _includeChains = new();

    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;

    public IReadOnlyList<Func<IQueryable<T>, IQueryable<T>>> IncludeChains => _includeChains;

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => _includes.Add(includeExpression);

    protected void AddIncludeChain(Func<IQueryable<T>, IQueryable<T>> chain) => _includeChains.Add(chain);
}
