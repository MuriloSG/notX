using System.Text;
using Dapper;
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
        INSERT INTO outbox_messages (id, type, payload, created_at, retry_count)
        VALUES (@Id, @Type, @Payload, @CreatedAt, @RetryCount)
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
        o.RetryCount
    };
}
