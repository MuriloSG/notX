using MediatR;
using notX.Domain.Enums;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Commands.CreateNotificationsBatch;

public sealed record NotificationItem(
    NotificationType Type,
    string Title,
    string Content,
    DateTime? ScheduledAt = null);

public sealed record CreateNotificationsBatchCommand(
    IReadOnlyList<NotificationItem> Notifications) : IRequest<Result<IReadOnlyList<Guid>>>;
