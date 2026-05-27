using notX.Application.Features.Notifications.Queries.GetNotifications;
using notX.Domain.Entities;
using notX.Domain.Enums;

namespace notX.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task InsertAsync(Notification notification, OutboxMessage outboxMessage);
    Task InsertBatchAsync(IEnumerable<Notification> notifications, IEnumerable<OutboxMessage> outboxMessages);
    Task<Notification?> GetByIdAsync(Guid id);
    Task UpdateStatusAsync(Guid id, NotificationStatus status, DateTime? sentAt = null);
    Task<(IEnumerable<Notification> Items, int TotalCount)> GetFilteredPagedAsync(NotificationFilter filter);
}
