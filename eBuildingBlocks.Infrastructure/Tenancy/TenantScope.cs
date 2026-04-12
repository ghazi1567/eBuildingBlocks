using eBuildingBlocks.Domain.Exceptions;
using eBuildingBlocks.Domain.Interfaces;

namespace eBuildingBlocks.Infrastructure.Tenancy;

public sealed class TenantScope : ITenantScope
{
    private static readonly AsyncLocal<Guid> Ambient = new();

    public Guid OverrideTenantId => Ambient.Value;

    public IDisposable Begin(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new TenantResolutionException(
                $"{nameof(ITenantScope)}.{nameof(Begin)} requires a non-empty tenant id.");
        }

        var previous = Ambient.Value;
        Ambient.Value = tenantId;
        return new ScopeDisposable(() => Ambient.Value = previous);
    }

    private sealed class ScopeDisposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
