using MediatR;
using Microsoft.AspNetCore.Mvc;
using notX.Application.Features.Applications.Commands.CreateApplication;
using notX.Application.Features.Applications.Queries.GetApplicationByApiKey;

namespace notX.Api.Controllers;

[ApiController]
[Route("applications")]
public sealed class ApplicationsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { result.Error.Code, result.Error.Message });

        return CreatedAtAction(nameof(GetByApiKey), new { apiKey = result.Value.ApiKey }, result.Value);
    }

    [HttpGet("{apiKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByApiKey(
        string apiKey,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetApplicationByApiKeyQuery(apiKey), cancellationToken);

        if (result.IsFailure)
            return NotFound(new { result.Error.Code, result.Error.Message });

        return Ok(result.Value);
    }
}
