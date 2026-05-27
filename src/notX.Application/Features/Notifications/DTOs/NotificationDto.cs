using notX.Domain.Enums;

namespace notX.Application.Features.Notifications.DTOs;

public sealed record NotificationDto(
    Guid Id,
    Guid ApplicationId,
    NotificationType Type,
    string Title,
    string Content,
    NotificationStatus Status,
    DateTime CreatedAt,
    DateTime? ScheduledAt,
    DateTime? SentAt);
