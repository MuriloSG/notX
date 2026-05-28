using System.Text.Json;
using MediatR;
using notX.Application.Features.Notifications.DTOs;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Domain.Entities;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Commands.CreateNotification;

internal sealed class CreateNotificationCommandHandler(
    INotificationRepository repository,
    ICurrentApplication currentApplication)
    : IRequestHandler<CreateNotificationCommand, Result<NotificationDto>>
{
    public async Task<Result<NotificationDto>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            currentApplication.ApplicationId,
            request.Type,
            request.Title,
            request.Content,
            request.Recipient,
            request.ScheduledAt);

        var outbox = new OutboxMessage(
            "NotificationCreated",
            JsonSerializer.Serialize(new
            {
                @event = "NotificationCreated",
                notificationId = notification.Id,
                type = notification.Type.ToString()
            }),
            notification.ScheduledAt);

        await repository.InsertAsync(notification, outbox);

        return Result.Success(ToDto(notification));
    }

    private static NotificationDto ToDto(Notification n) => new(
        n.Id, n.ApplicationId, n.Type, n.Title,
        n.Content, n.Recipient, n.Status, n.CreatedAt, n.ScheduledAt, n.SentAt);
}
