using System.Text.Json;
using MediatR;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Domain.Entities;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Commands.RetryNotification;

internal sealed class RetryNotificationCommandHandler(
    INotificationRepository repository,
    ICurrentApplication currentApplication)
    : IRequestHandler<RetryNotificationCommand, Result>
{
    public async Task<Result> Handle(
        RetryNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(request.Id);

        if (notification is null || notification.ApplicationId != currentApplication.ApplicationId)
            return Result.Failure(new Error("Notification.NotFound", $"Notification '{request.Id}' was not found."));

        if (!notification.CanRetry())
            return Result.Failure(new Error("Notification.CannotRetry", "Only failed or cancelled notifications can be retried."));

        notification.Retry();

        var outbox = new OutboxMessage(
            "NotificationCreated",
            JsonSerializer.Serialize(new
            {
                @event = "NotificationCreated",
                notificationId = notification.Id,
                type = notification.Type.ToString()
            }),
            scheduledAt: null);

        await repository.RetryAsync(notification.Id, outbox);

        return Result.Success();
    }
}
