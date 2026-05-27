using notX.Domain.Common;

namespace notX.Domain.Entities;

public class OutboxMessage : BaseEntity
{
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload)
    {
        Id = Guid.NewGuid();

        Type = type;
        Payload = payload;

        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void IncrementRetry(string? error = null)
    {
        RetryCount++;
        Error = error;
    }
}