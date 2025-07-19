using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.EventBus.Events
{
    public interface IEventSubscriber
    {
        void Subscribe<T, TConsumer>() where T : class where TConsumer : class, IConsumer<T>;
    }
}
