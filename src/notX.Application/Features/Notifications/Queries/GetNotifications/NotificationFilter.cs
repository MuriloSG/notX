using notX.Domain.Enums;

namespace notX.Application.Features.Notifications.Queries.GetNotifications;

public sealed record NotificationFilter(
    Guid? ApplicationId,
    NotificationType? Type,
    NotificationStatus? Status,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize);
