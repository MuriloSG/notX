using Dapper;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Domain.Entities;
using Npgsql;

namespace notX.Infrastructure.Persistence.Repositories;

internal sealed partial class OutboxRepository(IDbConnectionFactory connectionFactory) : IOutboxRepository
{
    public async Task<IEnumerable<OutboxMessage>> ClaimBatchAsync(
        Guid lockToken,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using var connection = (NpgsqlConnection)connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QueryAsync<OutboxMessage>(SqlClaimBatch, new { LockToken = lockToken, BatchSize = batchSize });
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(SqlMarkProcessed, new { Id = id });
    }

    public async Task IncrementRetryAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(SqlIncrementRetry, new { Id = id, Error = error });
    }
}
