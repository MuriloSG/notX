using MediatR;
using notX.Application.Features.Applications.DTOs;
using notX.Application.Interfaces.Repositories;
using notX.Shared.Results;
using AppEntity = notX.Domain.Entities.Application;

namespace notX.Application.Features.Applications.Commands.CreateApplication;

internal sealed class CreateApplicationCommandHandler(IApplicationRepository repository)
    : IRequestHandler<CreateApplicationCommand, Result<ApplicationDto>>
{
    public async Task<Result<ApplicationDto>> Handle(
        CreateApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var apiKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        var application = AppEntity.Create(request.Name, apiKey);

        await repository.InsertAsync(application);

        return Result.Success(new ApplicationDto(
            application.Id,
            application.Name,
            application.ApiKey,
            application.CreatedAt));
    }
}
