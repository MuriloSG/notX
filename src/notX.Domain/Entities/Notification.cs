using notX.Domain.Common;
using notX.Domain.Enums;

namespace notX.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid ApplicationId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Recipient { get; private set; } = default!;

    public string Subject { get; private set; } = default!;

    public string Body { get; private set; } = default!;

    public NotificationStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? SentAt { get; private set; }

    private Notification()
    {
    }

    public Notification(Guid applicationId, NotificationType type, string recipient, string subject, string body)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        Type = type;
        Recipient = recipient;
        Subject = subject;
        Body = body;
        Status = NotificationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing()
    {
        Status = NotificationStatus.Processing;
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = NotificationStatus.Failed;
    }

    public void Cancel()
    {
        Status = NotificationStatus.Cancelled;
    }
}