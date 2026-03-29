namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// SQL Server–specific batch claim for the outbox. Uses locking hints to reduce duplicate work across pods.
/// </summary>
internal static class SqlServerOutboxBatchClaim
{
    /// <summary>
    /// Claims up to <paramref name="BatchSize"/> rows by setting <c>LockedUntil</c> and returns the updated rows via OUTPUT.
    /// </summary>
    internal const string Sql = """
;WITH Next AS (
    SELECT TOP (@BatchSize) Id
    FROM OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE ProcessedAtUtc IS NULL
      AND AttemptCount < @MaxAttempts
      AND (LockedUntil IS NULL OR LockedUntil <= @Now)
    ORDER BY CreatedAtUtc
)
UPDATE o
SET LockedUntil = @LeaseUntil
OUTPUT
  inserted.Id,
  inserted.DomainEventId,
  inserted.EventName,
  inserted.PayloadJson,
  inserted.CreatedAtUtc,
  inserted.TenantId,
  inserted.AttemptCount,
  inserted.LastError,
  inserted.LockedUntil,
  inserted.ProcessedAtUtc
FROM OutboxMessages AS o
INNER JOIN Next AS n ON o.Id = n.Id;
""";
}
