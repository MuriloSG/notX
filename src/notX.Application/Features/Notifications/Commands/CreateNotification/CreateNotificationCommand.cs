using MediatR;
using notX.Application.Features.Notifications.DTOs;
using notX.Domain.Enums;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Commands.CreateNotification;

public sealed record CreateNotificationCommand(
    NotificationType Type,
    string Title,
    string Content,
    DateTime? ScheduledAt = null) : IRequest<Result<NotificationDto>>;
