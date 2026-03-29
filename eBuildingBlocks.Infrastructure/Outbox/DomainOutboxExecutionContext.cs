namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// When true for the current async flow, <see cref="DomainOutboxSaveChangesInterceptor"/> does not enqueue outbox rows.
/// Used by <see cref="Extensions.RepositoryExtensions.SaveChangesAndPublishDomainEventsInProcessAsync"/> so in-process-only
/// saves do not compete with the transactional outbox.
/// </summary>
internal static class DomainOutboxExecutionContext
{
    internal static readonly AsyncLocal<bool> SuppressTransactionalEnqueue = new();
}
