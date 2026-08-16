using BuildingBlocks.EventBus.Contracts;
using BuildingBlocks.EventBus.Events;
using eBuildingBlocks.Common.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eBuildingBlocks.Infrastructure.Tests.Outbox;

/// <summary>
/// Never instantiated — only used as a distinct <c>TDbContext</c> type argument so each test
/// gets its own copy of <c>OutboxProcessorBackgroundService&lt;TDbContext&gt;</c>'s static
/// one-time-warning guard field.
/// </summary>
internal sealed class FakeDbContext1 : DbContext;
internal sealed class FakeDbContext2 : DbContext;
internal sealed class FakeDbContext3 : DbContext;
internal sealed class FakeDbContext4 : DbContext;

internal sealed class FakeEventPublisher : IEventPublisher
{
    public int CallCount { get; private set; }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        CallCount++;
        return Task.CompletedTask;
    }

    public Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeOutboxIntegrationPublisher : IOutboxIntegrationPublisher
{
    public int CallCount { get; private set; }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        CallCount++;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingLogger : ILogger
{
    public int WarningCount { get; private set; }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning)
            WarningCount++;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
