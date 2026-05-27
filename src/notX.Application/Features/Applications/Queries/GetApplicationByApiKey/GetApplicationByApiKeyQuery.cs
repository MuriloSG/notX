using MediatR;
using notX.Application.Features.Applications.DTOs;
using notX.Shared.Results;

namespace notX.Application.Features.Applications.Queries.GetApplicationByApiKey;

public sealed record GetApplicationByApiKeyQuery(string ApiKey) : IRequest<Result<ApplicationDto>>;
