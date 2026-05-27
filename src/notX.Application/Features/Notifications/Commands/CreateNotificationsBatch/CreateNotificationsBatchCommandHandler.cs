using System.Text.Json;
using MediatR;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Domain.Entities;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Commands.CreateNotificationsBatch;

internal sealed class CreateNotificationsBatchCommandHandler(
    INotificationRepository repository,
    ICurrentApplication currentApplication)
    : IRequestHandler<CreateNotificationsBatchCommand, Result<IReadOnlyList<Guid>>>
{
    public async Task<Result<IReadOnlyList<Guid>>> Handle(
        CreateNotificationsBatchCommand request,
        CancellationToken cancellationToken)
    {
        var notifications = request.Notifications
            .Select(n => Notification.Create(
                currentApplication.ApplicationId,
                n.Type,
                n.Title,
                n.Content,
                n.ScheduledAt))
            .ToList();

        var outboxMessages = notifications
            .Select(n => new OutboxMessage(
                "NotificationCreated",
                JsonSerializer.Serialize(new
                {
                    @event = "NotificationCreated",
                    notificationId = n.Id,
                    type = n.Type.ToString()
                })))
            .ToList();

        await repository.InsertBatchAsync(notifications, outboxMessages);

        return Result.Success<IReadOnlyList<Guid>>(
            notifications.Select(n => n.Id).ToList());
    }
}
