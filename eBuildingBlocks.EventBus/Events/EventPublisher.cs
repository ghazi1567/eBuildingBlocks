using BuildingBlocks.EventBus.Contracts;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.EventBus.Events
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public EventPublisher(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        {
            await _publishEndpoint.Publish(@event, cancellationToken);
        }

        public async Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            await _publishEndpoint.Publish(@event, cancellationToken);
        }
    }

}
