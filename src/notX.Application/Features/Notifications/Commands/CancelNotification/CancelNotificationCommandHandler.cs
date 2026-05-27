using MediatR;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Domain.Enums;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Commands.CancelNotification;

internal sealed class CancelNotificationCommandHandler(
    INotificationRepository repository,
    ICurrentApplication currentApplication)
    : IRequestHandler<CancelNotificationCommand, Result>
{
    public async Task<Result> Handle(
        CancelNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(request.Id);

        if (notification is null || notification.ApplicationId != currentApplication.ApplicationId)
            return Result.Failure(new Error("Notification.NotFound", $"Notification '{request.Id}' was not found."));

        if (!notification.CanCancel())
            return Result.Failure(new Error("Notification.CannotCancel", "Only notifications that have not been sent can be cancelled."));

        notification.Cancel();

        await repository.UpdateStatusAsync(notification.Id, NotificationStatus.Cancelled);

        return Result.Success();
    }
}
