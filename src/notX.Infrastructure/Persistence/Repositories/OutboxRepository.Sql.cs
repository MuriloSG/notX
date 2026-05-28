namespace notX.Infrastructure.Persistence.Repositories;

internal sealed partial class OutboxRepository
{
    private const string OutboxColumns = """
        id, type, payload, created_at AS CreatedAt, processed_at AS ProcessedAt,
        scheduled_at AS ScheduledAt, lock_token AS LockToken, locked_at AS LockedAt,
        retry_count AS RetryCount, error
        """;

    private const string SqlClaimBatch = """
        UPDATE outbox_messages
        SET lock_token = @LockToken, locked_at = NOW() AT TIME ZONE 'UTC'
        WHERE id IN (
            SELECT id FROM outbox_messages
            WHERE processed_at IS NULL
              AND (lock_token IS NULL OR locked_at < NOW() AT TIME ZONE 'UTC' - INTERVAL '60 seconds')
              AND (scheduled_at IS NULL OR scheduled_at <= NOW() AT TIME ZONE 'UTC')
            ORDER BY created_at
            LIMIT @BatchSize
        )
        RETURNING id, type, payload,
                  created_at AS CreatedAt, processed_at AS ProcessedAt,
                  scheduled_at AS ScheduledAt, lock_token AS LockToken, locked_at AS LockedAt,
                  retry_count AS RetryCount, error
        """;

    private const string SqlMarkProcessed = """
        UPDATE outbox_messages
        SET processed_at = NOW() AT TIME ZONE 'UTC', lock_token = NULL, locked_at = NULL
        WHERE id = @Id
        """;

    private const string SqlIncrementRetry = """
        UPDATE outbox_messages
        SET retry_count = retry_count + 1, error = @Error, lock_token = NULL, locked_at = NULL
        WHERE id = @Id
        """;
}
