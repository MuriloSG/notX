using MediatR;
using Microsoft.AspNetCore.Mvc;
using notX.Api.Models;
using notX.Api.Services;
using notX.Application.Features.Notifications.Commands.CancelNotification;
using notX.Application.Features.Notifications.Commands.CreateNotification;
using notX.Application.Features.Notifications.Commands.CreateNotificationsBatch;
using notX.Application.Features.Notifications.Commands.RetryNotification;
using notX.Application.Features.Notifications.DTOs;
using notX.Application.Features.Notifications.Queries.GetNotifications;
using notX.Domain.Enums;
using notX.Infrastructure.Realtime;
using notX.Shared.Results;
using StackExchange.Redis;

namespace notX.Api.Controllers;

/// <summary>
/// Gerencia as notificações da aplicação autenticada.
/// Todos os endpoints exigem o header <c>X-Api-Key</c> com uma API key válida.
/// </summary>
[ApiController]
[Route("notifications")]
[Produces("application/json")]
[Tags("Notificações")]
public sealed class NotificationsController(
    IMediator mediator,
    IConnectionMultiplexer redis,
    CurrentApplication currentApplication) : ControllerBase
{
    /// <summary>Criar uma notificação individual.</summary>
    /// <remarks>
    /// Cria uma notificação com status <c>Pendente</c> e a enfileira para processamento via Outbox Pattern.
    ///
    /// **Tipos suportados:** `Email`, `Sms`
    ///
    /// **Exemplo de requisição:**
    /// ```json
    /// {
    ///   "type": "Email",
    ///   "title": "Bem-vindo!",
    ///   "content": "Obrigado por se cadastrar.",
    ///   "scheduledAt": null
    /// }
    /// ```
    /// </remarks>
    /// <param name="command">Dados da notificação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A notificação criada.</returns>
    /// <response code="201">Notificação criada e enfileirada para envio.</response>
    /// <response code="400">Erro de validação — tipo, título ou conteúdo inválido.</response>
    /// <response code="401">Header X-Api-Key ausente ou inválido.</response>
    [HttpPost]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ErrorResponse(result.Error.Code, result.Error.Message));

        await DashboardEvents.PublishAsync(redis, currentApplication.ApplicationId, "notification.created", result.Value);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Criar múltiplas notificações em lote.</summary>
    /// <remarks>
    /// Insere até 1000 notificações atomicamente em uma única transação. Todas iniciam com status <c>Pendente</c> e geram eventos individuais no Outbox.
    /// Se qualquer item falhar na validação, o lote inteiro é rejeitado.
    ///
    /// **Exemplo de requisição:**
    /// ```json
    /// {
    ///   "notifications": [
    ///     { "type": "Email", "title": "Olá", "content": "Mensagem 1" },
    ///     { "type": "Sms",   "title": "Alerta", "content": "Mensagem 2" }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="command">Lista de notificações (máx. 1000 itens).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de IDs das notificações criadas.</returns>
    /// <response code="201">Todas as notificações criadas com sucesso.</response>
    /// <response code="400">Erro de validação — lote vazio, acima de 1000 itens ou algum item inválido.</response>
    /// <response code="401">Header X-Api-Key ausente ou inválido.</response>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBatch(
        [FromBody] CreateNotificationsBatchCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ErrorResponse(result.Error.Code, result.Error.Message));

        return StatusCode(StatusCodes.Status201Created, new BatchCreatedResponse(result.Value));
    }

    /// <summary>Cancelar uma notificação.</summary>
    /// <remarks>
    /// Altera o status da notificação para <c>Cancelada</c>.
    /// Notificações com status <c>Enviada</c> não podem ser canceladas.
    ///
    /// **Transições de status permitidas:**
    ///
    /// | Status atual   | Permitido |
    /// |----------------|-----------|
    /// | Pendente       | ✅        |
    /// | Processando    | ✅        |
    /// | Falhou         | ✅        |
    /// | Cancelada      | ✅        |
    /// | Enviada        | ❌        |
    /// </remarks>
    /// <param name="id">ID da notificação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="204">Notificação cancelada com sucesso.</response>
    /// <response code="400">A notificação já foi enviada e não pode ser cancelada.</response>
    /// <response code="401">Header X-Api-Key ausente ou inválido.</response>
    /// <response code="404">Notificação não encontrada ou não pertence à aplicação autenticada.</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelNotificationCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Notification.NotFound")
                return NotFound(new ErrorResponse(result.Error.Code, result.Error.Message));

            return BadRequest(new ErrorResponse(result.Error.Code, result.Error.Message));
        }

        await DashboardEvents.PublishAsync(redis, currentApplication.ApplicationId, "notification.status_changed",
            new { id, status = NotificationStatus.Cancelled.ToString() });

        return NoContent();
    }

    /// <summary>Reenviar uma notificação com falha ou cancelada.</summary>
    /// <remarks>
    /// Redefine o status da notificação para <c>Pendente</c>, permitindo que o worker processe novamente.
    ///
    /// **Transições de status permitidas:**
    ///
    /// | Status atual   | Permitido |
    /// |----------------|-----------|
    /// | Falhou         | ✅        |
    /// | Cancelada      | ✅        |
    /// | Pendente       | ❌        |
    /// | Processando    | ❌        |
    /// | Enviada        | ❌        |
    /// </remarks>
    /// <param name="id">ID da notificação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <response code="204">Status redefinido para Pendente com sucesso.</response>
    /// <response code="400">A notificação não está em um estado que permite reenvio.</response>
    /// <response code="401">Header X-Api-Key ausente ou inválido.</response>
    /// <response code="404">Notificação não encontrada ou não pertence à aplicação autenticada.</response>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RetryNotificationCommand(id), cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Notification.NotFound")
                return NotFound(new ErrorResponse(result.Error.Code, result.Error.Message));

            return BadRequest(new ErrorResponse(result.Error.Code, result.Error.Message));
        }

        await DashboardEvents.PublishAsync(redis, currentApplication.ApplicationId, "notification.status_changed",
            new { id, status = NotificationStatus.Pending.ToString() });

        return NoContent();
    }

    /// <summary>Listar notificações com filtros e paginação.</summary>
    /// <remarks>
    /// Retorna apenas as notificações pertencentes à aplicação autenticada.
    /// Todos os filtros são opcionais e podem ser combinados.
    ///
    /// **Valores de NotificationType:** `Email`, `Sms`
    ///
    /// **Valores de NotificationStatus:** `Pending`, `Processing`, `Sent`, `Failed`, `Cancelled`
    ///
    /// **Exemplo:** `GET /notifications?status=Failed&amp;page=1&amp;pageSize=20`
    /// </remarks>
    /// <param name="type">Filtrar por tipo de notificação (opcional).</param>
    /// <param name="status">Filtrar por status (opcional).</param>
    /// <param name="from">Filtrar por data de criação — início, inclusive (opcional).</param>
    /// <param name="to">Filtrar por data de criação — fim, inclusive (opcional).</param>
    /// <param name="recipient">Filtrar por destinatário — busca parcial, sem distinção de maiúsculas (opcional).</param>
    /// <param name="page">Número da página, a partir de 1 (padrão: 1).</param>
    /// <param name="pageSize">Quantidade de itens por página, máx. 100 (padrão: 20).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista paginada de notificações.</returns>
    /// <response code="200">Resultado paginado retornado com sucesso.</response>
    /// <response code="401">Header X-Api-Key ausente ou inválido.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFiltered(
        [FromQuery] NotificationType? type,
        [FromQuery] NotificationStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? recipient,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetNotificationsQuery(type, status, from, to, recipient, page, pageSize),
            cancellationToken);

        return Ok(result.Value);
    }
}

/// <summary>Resposta retornada quando um lote de notificações é criado.</summary>
/// <param name="Ids">Lista de IDs das notificações criadas.</param>
public sealed record BatchCreatedResponse(IReadOnlyList<Guid> Ids);
