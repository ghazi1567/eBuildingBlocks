using eBuildingBlocks.Application.DependencyInjection;
using eBuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eBuildingBlocks.Infrastructure.Extensions;

/// <summary>
/// Bundles transactional outbox prerequisites for EF Core hosts: event type registry, domain outbox interceptor registration,
/// and the SQL Server–backed outbox processor hosted service.
/// </summary>
/// <remarks>
/// Register your <see cref="DbContext"/> with <see cref="DomainOutboxSaveChangesInterceptor"/> on the same options factory,
/// and map <see cref="DomainOutboxInfrastructureExtensions.ConfigureDomainOutbox"/> in <c>OnModelCreating</c>.
/// Register stable outbox keys at startup (e.g. <see cref="Microsoft.Extensions.Hosting.IHostedService"/>) before the first
/// <c>SaveChanges</c> that emits domain events.
/// </remarks>
public static class TransactionalOutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IEventTypeRegistry"/>, <see cref="DomainOutboxSaveChangesInterceptor"/>, and
    /// <see cref="OutboxProcessorBackgroundService{TDbContext}"/> for the given <typeparamref name="TDbContext"/>.
    /// </summary>
    public static IServiceCollection AddTransactionalOutboxInfrastructure<TDbContext>(
        this IServiceCollection services,
        IConfiguration? configuration = null)
        where TDbContext : DbContext
    {
        services.AddEventTypeRegistry();
        services.AddDomainOutboxInterceptor();
        services.AddOutboxProcessor<TDbContext>(configuration);
        return services;
    }
}
