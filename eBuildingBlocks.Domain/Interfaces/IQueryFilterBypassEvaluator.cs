namespace eBuildingBlocks.Domain.Interfaces;

/// <summary>
/// When <see cref="ISpecification{T}.IgnoreQueryFilters"/> is true, the repository only applies it if this evaluator allows bypass (e.g. admin or migration tooling).
/// </summary>
public interface IQueryFilterBypassEvaluator
{
    bool CanIgnoreGlobalQueryFilters { get; }
}
