using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.EventBus.Events;
using Dapper;
using eBuildingBlocks.Application.Eventing;
using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Polls <see cref="OutboxMessage"/> rows (SQL Server locking hints via raw SQL) and publishes via <see cref="IEventPublisher"/>.
/// Resolves CLR types using <see cref="IEventTypeRegistry"/>; unknown keys are poisoned without stopping the worker.
/// </summary>
public sealed class OutboxProcessorBackgroundService<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboxProcessorOptions> _options;
    private readonly ILogger<OutboxProcessorBackgroundService<TDbContext>> _logger;

    public OutboxProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxProcessorOptions> options,
        ILogger<OutboxProcessorBackgroundService<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox processor started for DbContext {DbContextType}. Poll: {Poll}",
            typeof(TDbContext).Name,
            _options.Value.PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor iteration failed for {DbContextType}", typeof(TDbContext).Name);
            }

            try
            {
                await Task.Delay(_options.Value.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox processor stopping for DbContext {DbContextType}", typeof(TDbContext).Name);
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<TDbContext>();
        var publisher = sp.GetRequiredService<IEventPublisher>();
        var registry = sp.GetRequiredService<IEventTypeRegistry>();
        var opt = _options.Value;

        List<OutboxMessage> batch;
        try
        {
            batch = await ClaimBatchSqlServerAsync(db, opt, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Outbox claim query failed (transient DB error possible); will retry on next poll");
            return;
        }

        if (batch.Count == 0)
            return;

        _logger.LogDebug("Outbox claimed {Count} message(s) for processing", batch.Count);

        foreach (var message in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PublishSingleAsync(db, publisher, registry, message, opt, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// SQL Server only: claims rows with <c>UPDLOCK, READPAST</c> and returns updated rows in one transaction.
    /// </summary>
    private async Task<List<OutboxMessage>> ClaimBatchSqlServerAsync(
        TDbContext db,
        OutboxProcessorOptions opt,
        CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        List<OutboxMessage> batch = [];

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);

            if (tx is not IInfrastructure<DbTransaction> infra)
                throw new InvalidOperationException(
                    "Outbox processor requires a relational provider (SQL Server). Register a relational DbContext.");

            var dbTran = infra.Instance ?? throw new InvalidOperationException("Underlying DbTransaction is not available.");

            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            var now = DateTime.UtcNow;
            var leaseUntil = now.Add(opt.LeaseDuration);
            var conn = db.Database.GetDbConnection();

            var rows = await conn.QueryAsync<OutboxMessage>(
                SqlServerOutboxBatchClaim.Sql,
                new
                {
                    BatchSize = opt.BatchSize,
                    MaxAttempts = opt.MaxAttempts,
                    Now = now,
                    LeaseUntil = leaseUntil
                },
                dbTran).ConfigureAwait(false);

            batch = rows.ToList();

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return batch;
    }

    private async Task PublishSingleAsync(
        TDbContext db,
        IEventPublisher publisher,
        IEventTypeRegistry registry,
        OutboxMessage message,
        OutboxProcessorOptions opt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.EventName))
        {
            await MarkUnknownEventNamePoisonAsync(db, message, "Empty EventName", opt, cancellationToken).ConfigureAwait(false);
            return;
        }

        var eventType = registry.Resolve(message.EventName);
        if (eventType is null)
        {
            _logger.LogCritical(
                "Outbox message {OutboxId} has unknown EventName '{EventName}'. Poisoning row.",
                message.Id,
                message.EventName);
            await MarkUnknownEventNamePoisonAsync(
                db,
                message,
                $"Unregistered event name: {message.EventName}",
                opt,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        object? payload;
        try
        {
            payload = JsonSerializer.Deserialize(message.PayloadJson, eventType, DeserializeOptions);
            if (payload is null)
                throw new InvalidOperationException("Deserialized payload is null.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Outbox message {OutboxId} deserialization failed (attempt {Attempt})",
                message.Id,
                message.AttemptCount);
            await RecordFailureAsync(db, message, ex, opt, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            await OutboxPublisherDispatch.PublishAsync(publisher, payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Outbox message {OutboxId} publish failed (attempt {Attempt})",
                message.Id,
                message.AttemptCount);
            await RecordFailureAsync(db, message, ex, opt, cancellationToken).ConfigureAwait(false);
            return;
        }

        message.ProcessedAtUtc = DateTime.UtcNow;
        message.LastError = null;
        message.LockedUntil = null;

        db.Set<OutboxMessage>().Update(message);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Outbox message {OutboxId} published (domain event {EventId}, event {EventName})",
            message.Id,
            message.DomainEventId,
            message.EventName);
    }

    private async Task MarkUnknownEventNamePoisonAsync(
        TDbContext db,
        OutboxMessage message,
        string reason,
        OutboxProcessorOptions opt,
        CancellationToken cancellationToken)
    {
        message.AttemptCount = opt.MaxAttempts;
        message.LastError = TruncateText(reason, opt.LastErrorMaxLength);
        message.LockedUntil = null;

        db.Set<OutboxMessage>().Update(message);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist poison state for outbox message {OutboxId}", message.Id);
        }
    }

    private async Task RecordFailureAsync(
        TDbContext db,
        OutboxMessage message,
        Exception exception,
        OutboxProcessorOptions opt,
        CancellationToken cancellationToken)
    {
        message.AttemptCount++;
        message.LastError = TruncateError(exception, opt.LastErrorMaxLength);
        message.LockedUntil = DateTime.UtcNow.Add(ComputeBackoffAfterFailure(message.AttemptCount, opt));

        db.Set<OutboxMessage>().Update(message);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist outbox failure state for message {OutboxId}",
                message.Id);
        }

        if (message.AttemptCount >= opt.MaxAttempts)
        {
            _logger.LogError(
                "Outbox message {OutboxId} exceeded MaxAttempts ({Max}). Poison. Last error: {Error}",
                message.Id,
                opt.MaxAttempts,
                message.LastError);
        }
    }

    private static TimeSpan ComputeBackoffAfterFailure(int attemptCount, OutboxProcessorOptions opt)
    {
        var factor = Math.Pow(2, Math.Max(0, attemptCount - 1));
        var seconds = Math.Min(opt.MaxBackoffSeconds, opt.BaseBackoffSeconds * factor);
        return TimeSpan.FromSeconds(seconds);
    }

    private static string TruncateError(Exception exception, int maxLength)
    {
        var text = $"{exception.GetType().FullName}: {exception.Message}";
        return TruncateText(text, maxLength);
    }

    private static string TruncateText(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength];
}