using System.Collections.Concurrent;

namespace notX.Api.Realtime;

public sealed class SseConnectionLimiter
{
    public const int MaxPerApplication = 5;

    private readonly ConcurrentDictionary<Guid, int> _counts = new();

    public bool TryAcquire(Guid applicationId)
    {
        while (true)
        {
            var current = _counts.GetOrAdd(applicationId, 0);
            if (current >= MaxPerApplication) return false;
            if (_counts.TryUpdate(applicationId, current + 1, current)) return true;
        }
    }

    public void Release(Guid applicationId)
    {
        _counts.AddOrUpdate(applicationId, 0, (_, v) => Math.Max(0, v - 1));
    }
}
