using eBuildingBlocks.Application.Eventing;
using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Registration and model configuration for the domain integration outbox.
/// </summary>
public static class DomainOutboxInfrastructureExtensions
{
    /// <summary>
    /// Maps <see cref="OutboxMessage"/> to the <c>OutboxMessages</c> table. Call from <see cref="DbContext.OnModelCreating"/>.
    /// </summary>
    public static void ConfigureDomainOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DomainEventId).IsRequired();
            entity.HasIndex(e => e.DomainEventId);
            entity.Property(e => e.EventName).HasMaxLength(512).IsRequired();
            entity.HasIndex(e => e.EventName);
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.AttemptCount).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.LastError).HasMaxLength(4000);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => e.ProcessedAtUtc);
            entity.HasIndex(e => e.LockedUntil);
        });
    }

    /// <summary>
    /// Registers <see cref="DomainOutboxSaveChangesInterceptor"/> as a singleton. Add it to your DbContext with
    /// <c>options.AddInterceptors(serviceProvider.GetRequiredService&lt;DomainOutboxSaveChangesInterceptor&gt;())</c>.
    /// Requires <see cref="IEventTypeRegistry"/> via <c>AddEventTypeRegistry()</c> and a stable name per event type via <see cref="IEventTypeRegistry.Register{TEvent}"/>.
    /// </summary>
    public static IServiceCollection AddDomainOutboxInterceptor(this IServiceCollection services)
    {
        services.AddSingleton<DomainOutboxSaveChangesInterceptor>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="OutboxProcessorBackgroundService{TDbContext}"/> as a hosted service (SQL Server locking + <see cref="IEventTypeRegistry"/>).
    /// Requires <typeparamref name="TDbContext"/> (scoped), <see cref="IEventPublisher"/>, and <see cref="IEventTypeRegistry"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional configuration; binds section <see cref="OutboxProcessorOptions.SectionName"/>.</param>
    public static IServiceCollection AddOutboxProcessor<TDbContext>(this IServiceCollection services, IConfiguration? configuration = null)
        where TDbContext : DbContext
    {
        if (configuration != null)
            services.Configure<OutboxProcessorOptions>(configuration.GetSection(OutboxProcessorOptions.SectionName));
        else
            services.AddOptions<OutboxProcessorOptions>();

        services.AddHostedService<OutboxProcessorBackgroundService<TDbContext>>();
        return services;
    }
}
