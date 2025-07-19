using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.EventBus.Events
{
    public static class MassTransitExtensions
    {
        public static void AddEventBus(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IEventPublisher, EventPublisher>();
            services.AddScoped<IEventSubscriber, EventSubscriber>();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(config["RabbitMQSettings:HostName"], "/", h =>
                    {
                        h.Username(config["RabbitMQSettings:Username"]);
                        h.Password(config["RabbitMQSettings:Password"]);
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });
        }
    }

}
