using MediatR;
using Microsoft.AspNetCore.Mvc;
using notX.Api.Models;
using notX.Application.Features.Applications.Commands.CreateApplication;
using notX.Application.Features.Applications.DTOs;
using notX.Application.Features.Applications.Queries.GetApplicationByApiKey;

namespace notX.Api.Controllers;

/// <summary>
/// Gerencia as aplicações (tenants) que utilizam a plataforma notX.
/// Estes endpoints não exigem autenticação via API key.
/// </summary>
[ApiController]
[Route("applications")]
[Produces("application/json")]
[Tags("Aplicações")]
public sealed class ApplicationsController(IMediator mediator) : ControllerBase
{
    /// <summary>Cadastrar uma nova aplicação.</summary>
    /// <remarks>
    /// Cria uma nova aplicação tenant e retorna a API key gerada automaticamente.
    /// Guarde a <c>apiKey</c> com segurança — ela é obrigatória para autenticar todas as requisições de notificação via o header <c>X-Api-Key</c>.
    ///
    /// **Exemplo de requisição:**
    /// ```json
    /// { "name": "Meu App" }
    /// ```
    /// </remarks>
    /// <param name="command">Nome da aplicação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A aplicação criada com a API key gerada.</returns>
    /// <response code="201">Aplicação criada com sucesso.</response>
    /// <response code="400">Erro de validação — nome ausente ou acima de 200 caracteres.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ErrorResponse(result.Error.Code, result.Error.Message));

        return CreatedAtAction(nameof(GetByApiKey), new { apiKey = result.Value.ApiKey }, result.Value);
    }

    /// <summary>Buscar uma aplicação pela API key.</summary>
    /// <remarks>
    /// Útil para verificar se uma API key é válida e obter os dados da aplicação associada.
    /// </remarks>
    /// <param name="apiKey">A API key da aplicação (string hexadecimal de 64 caracteres).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os dados da aplicação.</returns>
    /// <response code="200">Aplicação encontrada.</response>
    /// <response code="404">Nenhuma aplicação encontrada para a API key informada.</response>
    [HttpGet("{apiKey}")]
    [ProducesResponseType(typeof(ApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByApiKey(
        string apiKey,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetApplicationByApiKeyQuery(apiKey), cancellationToken);

        if (result.IsFailure)
            return NotFound(new ErrorResponse(result.Error.Code, result.Error.Message));

        return Ok(result.Value);
    }
}
