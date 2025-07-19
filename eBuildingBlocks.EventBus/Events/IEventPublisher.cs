using BuildingBlocks.EventBus.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.EventBus.Events
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event) where T : class;
        Task PublishAsync(IntegrationEvent @event);
    }
}
