using MediatR;
using notX.Shared.Results;

namespace notX.Application.Features.Notifications.Commands.RetryNotification;

public sealed record RetryNotificationCommand(Guid Id) : IRequest<Result>;
