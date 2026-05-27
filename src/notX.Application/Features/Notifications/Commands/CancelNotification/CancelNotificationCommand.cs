using MediatR;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Commands.CancelNotification;

public sealed record CancelNotificationCommand(Guid Id) : IRequest<Result>;
