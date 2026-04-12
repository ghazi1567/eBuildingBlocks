using BuildingBlocks.EventBus.Enums;

namespace BuildingBlocks.EventBus.Contracts
{
    public class IntegrationEvent
    {
        public Guid EventId { get; set; } = Guid.CreateVersion7();

        /// <summary>Required when multi-tenancy is enabled so consumers can call <c>ITenantScope.Begin</c> with this id.</summary>
        public Guid TenantId { get; set; }

        public EventType EventType { get; set; }  // e.g., "UserProjectAssigned"
        public Guid CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public object Payload { get; set; }    // Your actual event data
    }
}
