using System.Reflection;
using eBuildingBlocks.Common.Outbox;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.EventBus.Events;

/// <summary>
/// Registers <see cref="IEventPublisher"/> / <see cref="IEventSubscriber"/> and MassTransit with either in-memory
/// (local dev) or RabbitMQ transport, discovering consumers from the supplied assemblies.
/// </summary>
public static class IntegrationMassTransitServiceCollectionExtensions
{
    /// <summary>
    /// Adds MassTransit, wires <see cref="IEventPublisher"/> to <see cref="EventPublisher"/>, and registers consumers from
    /// <paramref name="consumerAssemblies"/> (pass your API or worker assembly that contains <c>IConsumer&lt;&gt;</c> types).
    /// </summary>
    /// <param name="useInMemory">When null, reads <c>MassTransit:UseInMemory</c> (default true).</param>
    public static IServiceCollection AddIntegrationMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        IEnumerable<Assembly> consumerAssemblies,
        bool? useInMemory = null)
    {
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<IOutboxIntegrationPublisher>(sp => (IOutboxIntegrationPublisher)sp.GetRequiredService<IEventPublisher>());
        services.AddScoped<IEventSubscriber, EventSubscriber>();

        // Avoid IConfiguration.GetValue: MassTransit brings conflicting extension methods.
        var inMemory = useInMemory ?? ReadUseInMemoryTransport(configuration);
        var assemblies = consumerAssemblies as Assembly[] ?? consumerAssemblies.ToArray();

        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            foreach (var assembly in assemblies.Where(a => a != null))
                x.AddConsumers(assembly);

            if (inMemory)
            {
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            }
            else
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(
                        configuration["RabbitMQSettings:HostName"] ?? "localhost",
                        "/",
                        h =>
                        {
                            h.Username(configuration["RabbitMQSettings:Username"] ?? "guest");
                            h.Password(configuration["RabbitMQSettings:Password"] ?? "guest");
                        });

                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }

    private static bool ReadUseInMemoryTransport(IConfiguration configuration)
    {
        var s = configuration["MassTransit:UseInMemory"];
        if (string.IsNullOrWhiteSpace(s)) return true;
        return bool.TryParse(s, out var b) && b;
    }

    /// <summary>Convenience overload for a single consumer assembly.</summary>
    public static IServiceCollection AddIntegrationMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly consumerAssembly,
        bool? useInMemory = null) =>
        services.AddIntegrationMassTransit(configuration, [consumerAssembly], useInMemory);
}
