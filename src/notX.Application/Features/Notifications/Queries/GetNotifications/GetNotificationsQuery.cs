using MediatR;
using notX.Application.Features.Notifications.DTOs;
using notX.Domain.Enums;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(
    NotificationType? Type = null,
    NotificationStatus? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<NotificationDto>>>;
