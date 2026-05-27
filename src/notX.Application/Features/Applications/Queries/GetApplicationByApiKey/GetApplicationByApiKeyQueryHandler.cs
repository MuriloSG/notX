using MediatR;
using notX.Application.Features.Applications.DTOs;
using notX.Application.Interfaces.Repositories;
using notX.Shared.Results;

namespace notX.Application.Features.Applications.Queries.GetApplicationByApiKey;

internal sealed class GetApplicationByApiKeyQueryHandler(IApplicationRepository repository)
    : IRequestHandler<GetApplicationByApiKeyQuery, Result<ApplicationDto>>
{
    public async Task<Result<ApplicationDto>> Handle(
        GetApplicationByApiKeyQuery request,
        CancellationToken cancellationToken)
    {
        var application = await repository.GetByApiKeyAsync(request.ApiKey);

        if (application is null)
            return Result.Failure<ApplicationDto>(
                new Error("Application.NotFound", "Application not found."));

        return Result.Success(new ApplicationDto(
            application.Id,
            application.Name,
            application.ApiKey,
            application.CreatedAt));
    }
}
