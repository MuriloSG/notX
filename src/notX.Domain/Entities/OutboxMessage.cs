using notX.Domain.Common;

namespace notX.Domain.Entities;

public class OutboxMessage : BaseEntity
{
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;

    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? ScheduledAt { get; private set; }

    public Guid? LockToken { get; private set; }
    public DateTime? LockedAt { get; private set; }

    public int RetryCount { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload, DateTime? scheduledAt = null)
    {
        Id = Guid.NewGuid();

        Type = type;
        Payload = payload;
        ScheduledAt = scheduledAt;

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