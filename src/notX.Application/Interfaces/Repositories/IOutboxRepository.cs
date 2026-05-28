using notX.Domain.Entities;

namespace notX.Application.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task<IEnumerable<OutboxMessage>> ClaimBatchAsync(Guid lockToken, int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task IncrementRetryAsync(Guid id, string error, CancellationToken cancellationToken = default);
}
