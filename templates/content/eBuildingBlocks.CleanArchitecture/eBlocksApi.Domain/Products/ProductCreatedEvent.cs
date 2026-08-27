using eBuildingBlocks.Domain.Models;

namespace eBlocksApi.Domain.Products;

// Raised when a product is created. This starter doesn't wire up dispatch by default —
// see docs/examples/reliable-events-with-outbox.md in the eBuildingBlocks repo for how to
// deliver it via the transactional outbox, or the in-process IEventBus for same-process handlers.
public sealed record ProductCreatedEvent(Guid ProductId, string Name) : BaseDomainEvent(Guid.Empty);
