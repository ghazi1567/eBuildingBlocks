namespace eBuildingBlocks.Domain.Interfaces;

/// <summary>
/// Unit-of-work boundary for persisting changes (typically backed by <c>DbContext.SaveChangesAsync</c>).
/// Prefer injecting this instead of calling save on a repository interface.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
