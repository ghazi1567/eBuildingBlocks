namespace eBuildingBlocks.Domain.Exceptions;

/// <summary>
/// Thrown when multi-tenancy is enabled but no non-empty tenant could be resolved for the current execution context.
/// </summary>
public sealed class TenantResolutionException : InvalidOperationException
{
    public TenantResolutionException(string message)
        : base(message)
    {
    }
}
