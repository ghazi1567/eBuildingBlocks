namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Configuration for <see cref="OutboxProcessorBackgroundService{TDbContext}"/>.
/// </summary>
public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    /// <summary>Polling interval when the outbox is idle or after each batch attempt.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public int BatchSize { get; set; } = 20;

    /// <summary>After this many failures, the message is no longer picked up (poison); monitor via logs/metrics.</summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>Lease duration after claiming a batch so competing hosts skip those rows.</summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Base for exponential backoff after failures (seconds).</summary>
    public int BaseBackoffSeconds { get; set; } = 15;

    public int MaxBackoffSeconds { get; set; } = 3600;

    /// <summary>Max characters stored in <see cref="OutboxMessage.LastError"/>.</summary>
    public int LastErrorMaxLength { get; set; } = 4000;
}
