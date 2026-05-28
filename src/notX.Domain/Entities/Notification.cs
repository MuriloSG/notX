using notX.Domain.Common;
using notX.Domain.Enums;

namespace notX.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid ApplicationId { get; internal set; }
    public NotificationType Type { get; internal set; }
    public string Title { get; internal set; } = default!;
    public string Content { get; internal set; } = default!;
    public string Recipient { get; internal set; } = default!;
    public NotificationStatus Status { get; internal set; }
    public DateTime CreatedAt { get; internal set; }
    public DateTime? ScheduledAt { get; internal set; }
    public DateTime? SentAt { get; internal set; }

    private Notification() { }

    public static Notification Create(
        Guid applicationId,
        NotificationType type,
        string title,
        string content,
        string recipient,
        DateTime? scheduledAt = null) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        Type = type,
        Title = title,
        Content = content,
        Recipient = recipient,
        Status = NotificationStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        ScheduledAt = scheduledAt
    };

    public bool CanCancel() => Status != NotificationStatus.Sent;
    public bool CanRetry() => Status is NotificationStatus.Failed or NotificationStatus.Cancelled;

    public void Cancel() => Status = NotificationStatus.Cancelled;
    public void Retry() => Status = NotificationStatus.Pending;

    public void MarkAsProcessing() => Status = NotificationStatus.Processing;
    public void MarkAsSent() { Status = NotificationStatus.Sent; SentAt = DateTime.UtcNow; }
    public void MarkAsFailed() => Status = NotificationStatus.Failed;
}
