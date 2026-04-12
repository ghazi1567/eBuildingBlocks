using eBuildingBlocks.Domain.Interfaces;

namespace eBuildingBlocks.Infrastructure.Implementations;

public sealed class DenyQueryFilterBypassEvaluator : IQueryFilterBypassEvaluator
{
    public bool CanIgnoreGlobalQueryFilters => false;
}
