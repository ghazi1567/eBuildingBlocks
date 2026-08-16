namespace eBuildingBlocks.Common.Outbox;

/// <summary>
/// Broker-agnostic contract used by the transactional outbox processor to publish a
/// dequeued domain event as an integration event. Implement this against whichever
/// message bus you use (MassTransit, Azure Service Bus, Kafka, etc.) and register it
/// in DI as <c>IOutboxIntegrationPublisher</c>.
///
/// eBuildingBlocks.EventBus ships a ready-made MassTransit implementation — see
/// <c>AddIntegrationMassTransit</c>, which registers it automatically.
/// </summary>
public interface IOutboxIntegrationPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
}
