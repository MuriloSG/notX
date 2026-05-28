namespace notX.Infrastructure.Persistence.Repositories;

internal sealed partial class NotificationRepository
{
    private const string NotificationColumns = """
        id, application_id AS ApplicationId, type, title, content, recipient,
        status, created_at AS CreatedAt, scheduled_at AS ScheduledAt, sent_at AS SentAt
        """;

    private const string InsertNotificationSql = """
        INSERT INTO notifications (id, application_id, type, title, content, recipient, status, created_at, scheduled_at, sent_at)
        VALUES (@Id, @ApplicationId, @Type, @Title, @Content, @Recipient, @Status, @CreatedAt, @ScheduledAt, @SentAt)
        """;

    private const string InsertOutboxSql = """
        INSERT INTO outbox_messages (id, type, payload, created_at, scheduled_at, retry_count)
        VALUES (@Id, @Type, @Payload, @CreatedAt, @ScheduledAt, @RetryCount)
        """;

    private const string SqlGetById =
        $"SELECT {NotificationColumns} FROM notifications WHERE id = @Id";

    private const string SqlUpdateStatus = """
        UPDATE notifications SET status = @Status, sent_at = @SentAt WHERE id = @Id
        """;

    private const string SqlRetryUpdate = """
        UPDATE notifications SET status = @Status, sent_at = NULL WHERE id = @Id
        """;

    private string SqlDashboardSnapshot => $"""
        SELECT status, COUNT(*) AS Count
        FROM notifications
        WHERE application_id = @ApplicationId
        GROUP BY status;

        SELECT type, COUNT(*) AS Count
        FROM notifications
        WHERE application_id = @ApplicationId
        GROUP BY type;

        SELECT date_trunc('hour', created_at) AS Hour, status, COUNT(*) AS Count
        FROM notifications
        WHERE application_id = @ApplicationId AND created_at >= @Since
        GROUP BY Hour, status
        ORDER BY Hour;

        SELECT {NotificationColumns}
        FROM notifications
        WHERE application_id = @ApplicationId
        ORDER BY created_at DESC
        LIMIT 20;
        """;
}
