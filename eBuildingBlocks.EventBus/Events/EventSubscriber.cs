using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.EventBus.Events
{
    public class EventSubscriber : IEventSubscriber
    {
        private readonly IBusRegistrationConfigurator _configurator;

        public EventSubscriber()
        {
            //_configurator = configurator;
        }

        public void Subscribe<T, TConsumer>()
            where T : class
            where TConsumer : class, IConsumer<T>
        {
            //_configurator.AddConsumer<TConsumer>();
        }
    }

}
