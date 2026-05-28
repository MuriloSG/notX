using System.Text;
using Dapper;
using notX.Application.Features.Notifications.DTOs;
using notX.Application.Features.Notifications.Queries.GetNotifications;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Domain.Entities;
using notX.Domain.Enums;
using Npgsql;

namespace notX.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(IDbConnectionFactory connectionFactory)
    : INotificationRepository
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

    public async Task InsertAsync(Notification notification, OutboxMessage outboxMessage)
    {
        using var connection = (NpgsqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(InsertNotificationSql, ToParams(notification), transaction);
        await connection.ExecuteAsync(InsertOutboxSql, ToOutboxParams(outboxMessage), transaction);

        await transaction.CommitAsync();
    }

    public async Task InsertBatchAsync(
        IEnumerable<Notification> notifications,
        IEnumerable<OutboxMessage> outboxMessages)
    {
        using var connection = (NpgsqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(InsertNotificationSql, notifications.Select(ToParams), transaction);
        await connection.ExecuteAsync(InsertOutboxSql, outboxMessages.Select(ToOutboxParams), transaction);

        await transaction.CommitAsync();
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        var sql = $"SELECT {NotificationColumns} FROM notifications WHERE id = @Id";

        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Notification>(sql, new { Id = id });
    }

    public async Task UpdateStatusAsync(Guid id, NotificationStatus status, DateTime? sentAt = null)
    {
        const string sql = """
            UPDATE notifications SET status = @Status, sent_at = @SentAt WHERE id = @Id
            """;

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, Status = status, SentAt = sentAt });
    }

    public async Task<(IEnumerable<Notification> Items, int TotalCount)> GetFilteredPagedAsync(
        NotificationFilter filter)
    {
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        if (filter.ApplicationId.HasValue)
        {
            where.Append(" AND application_id = @ApplicationId");
            parameters.Add("ApplicationId", filter.ApplicationId.Value);
        }

        if (filter.Type.HasValue)
        {
            where.Append(" AND type = @Type");
            parameters.Add("Type", (int)filter.Type.Value);
        }

        if (filter.Status.HasValue)
        {
            where.Append(" AND status = @Status");
            parameters.Add("Status", (int)filter.Status.Value);
        }

        if (filter.From.HasValue)
        {
            where.Append(" AND created_at >= @From");
            parameters.Add("From", filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            where.Append(" AND created_at <= @To");
            parameters.Add("To", filter.To.Value);
        }

        var offset = (filter.Page - 1) * filter.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", filter.PageSize);

        var sql = $"""
            SELECT {NotificationColumns}
            FROM notifications
            {where}
            ORDER BY created_at DESC
            OFFSET @Offset LIMIT @PageSize;

            SELECT COUNT(*) FROM notifications {where};
            """;

        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var items = await multi.ReadAsync<Notification>();
        var totalCount = await multi.ReadFirstAsync<int>();

        return (items, totalCount);
    }

    public async Task<DashboardSnapshotDto> GetDashboardSnapshotAsync(Guid applicationId)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var sql = $"""
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

        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(sql, new { ApplicationId = applicationId, Since = since });

        var statusRows = await multi.ReadAsync<(int Status, int Count)>();
        var typeRows = await multi.ReadAsync<(int Type, int Count)>();
        var seriesRows = await multi.ReadAsync<(DateTime Hour, int Status, int Count)>();
        var recent = (await multi.ReadAsync<Notification>()).ToList();

        var statusCounts = statusRows.ToDictionary(r => (NotificationStatus)r.Status, r => r.Count);
        var typeCounts = typeRows.ToDictionary(r => (NotificationType)r.Type, r => r.Count);

        var status = new StatusCountsDto(
            statusCounts.GetValueOrDefault(NotificationStatus.Pending),
            statusCounts.GetValueOrDefault(NotificationStatus.Processing),
            statusCounts.GetValueOrDefault(NotificationStatus.Sent),
            statusCounts.GetValueOrDefault(NotificationStatus.Failed),
            statusCounts.GetValueOrDefault(NotificationStatus.Cancelled));

        var types = new TypeCountsDto(
            typeCounts.GetValueOrDefault(NotificationType.Email),
            typeCounts.GetValueOrDefault(NotificationType.Sms));

        var denominator = status.Sent + status.Failed;
        var successRate = denominator == 0 ? 0d : Math.Round((double)status.Sent / denominator * 100, 2);

        var buckets = seriesRows
            .GroupBy(r => r.Hour)
            .Select(g => new TimeBucketDto(
                g.Key,
                g.Where(x => x.Status == (int)NotificationStatus.Sent).Sum(x => x.Count),
                g.Where(x => x.Status == (int)NotificationStatus.Failed).Sum(x => x.Count)))
            .ToList();

        var recentDtos = recent.Select(n => new NotificationDto(
            n.Id, n.ApplicationId, n.Type, n.Title, n.Content, n.Recipient,
            n.Status, n.CreatedAt, n.ScheduledAt, n.SentAt)).ToList();

        return new DashboardSnapshotDto(status, types, successRate, buckets, recentDtos);
    }

    private static object ToParams(Notification n) => new
    {
        n.Id,
        n.ApplicationId,
        Type = (int)n.Type,
        n.Title,
        n.Content,
        n.Recipient,
        Status = (int)n.Status,
        n.CreatedAt,
        n.ScheduledAt,
        n.SentAt
    };

    private static object ToOutboxParams(OutboxMessage o) => new
    {
        o.Id,
        o.Type,
        o.Payload,
        o.CreatedAt,
        o.ScheduledAt,
        o.RetryCount
    };
}
