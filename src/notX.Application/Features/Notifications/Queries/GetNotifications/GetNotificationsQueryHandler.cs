using MediatR;
using notX.Application.Features.Notifications.DTOs;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Queries.GetNotifications;

internal sealed class GetNotificationsQueryHandler(
    INotificationRepository repository,
    ICurrentApplication currentApplication)
    : IRequestHandler<GetNotificationsQuery, Result<PagedResult<NotificationDto>>>
{
    public async Task<Result<PagedResult<NotificationDto>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new NotificationFilter(
            currentApplication.ApplicationId,
            request.Type,
            request.Status,
            request.From,
            request.To,
            request.Page,
            request.PageSize);

        var (items, totalCount) = await repository.GetFilteredPagedAsync(filter);

        var dtos = items.Select(n => new NotificationDto(
            n.Id, n.ApplicationId, n.Type, n.Title,
            n.Content, n.Status, n.CreatedAt, n.ScheduledAt, n.SentAt));

        return Result.Success(new PagedResult<NotificationDto>(
            dtos, totalCount, request.Page, request.PageSize));
    }
}
