using MediatR;
using notX.Application.Features.Applications.DTOs;
using notX.Shared.Results;

namespace notX.Application.Features.Applications.Commands.CreateApplication;

public sealed record CreateApplicationCommand(string Name) : IRequest<Result<ApplicationDto>>;
