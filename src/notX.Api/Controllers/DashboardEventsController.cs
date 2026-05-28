using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using notX.Api.Realtime;
using notX.Api.Services;
using notX.Application.Interfaces.Repositories;
using notX.Infrastructure.Realtime;
using StackExchange.Redis;

namespace notX.Api.Controllers;

/// <summary>
/// Stream SSE de eventos em tempo real para o dashboard da aplicação autenticada.
/// Exige header <c>X-Api-Key</c> válido.
/// </summary>
[ApiController]
[Route("events")]
[Tags("Eventos em tempo real")]
public sealed class DashboardEventsController : ControllerBase
{
    private static readonly TimeSpan KeepaliveInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MetricsInterval = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Abre uma conexão SSE de eventos do dashboard.</summary>
    /// <remarks>
    /// **Content-Type:** `text/event-stream`. Cada frame segue o formato SSE padrão:
    /// ```
    /// event: &lt;tipo&gt;
    /// data: &lt;json&gt;
    ///
    /// ```
    ///
    /// **Eventos emitidos:**
    ///
    /// `notification.created` — emitido ao criar uma notificação.
    /// ```json
    /// {
    ///   "id": "guid",
    ///   "applicationId": "guid",
    ///   "type": "Email",
    ///   "title": "string",
    ///   "content": "string",
    ///   "recipient": "string",
    ///   "status": "Pending",
    ///   "createdAt": "2026-05-27T12:00:00Z",
    ///   "scheduledAt": null,
    ///   "sentAt": null
    /// }
    /// ```
    ///
    /// `notification.status_changed` — emitido em cancelamento, retry, envio e falha.
    /// ```json
    /// {
    ///   "id": "guid",
    ///   "status": "Sent",          // Pending | Processing | Sent | Failed | Cancelled
    ///   "sentAt": "2026-05-27T12:00:01Z", // presente quando status = Sent
    ///   "error": "string"                  // presente quando status = Failed
    /// }
    /// ```
    ///
    /// `metrics.snapshot` — snapshot inicial ao conectar e em seguida a cada 5s.
    /// ```json
    /// {
    ///   "status":      { "pending": 0, "processing": 0, "sent": 0, "failed": 0, "cancelled": 0 },
    ///   "types":       { "email": 0, "sms": 0 },
    ///   "successRate": 0.0,             // percentual: sent / (sent + failed) * 100
    ///   "last24h": [                    // série horária das últimas 24h
    ///     { "hour": "2026-05-27T11:00:00Z", "sent": 0, "failed": 0 }
    ///   ],
    ///   "recent": [                     // últimas 20 notificações por createdAt desc
    ///     { "id": "guid", "type": "Email", "title": "...", "recipient": "...", "status": "Sent", "createdAt": "...", "sentAt": "..." }
    ///   ]
    /// }
    /// ```
    ///
    /// **Heartbeat:** comment `:keepalive` enviado a cada 20s para manter a conexão viva.
    ///
    /// **Sem replay:** eventos ocorridos enquanto o cliente está desconectado não são reenviados;
    /// na reconexão o cliente recebe apenas um novo `metrics.snapshot` e os eventos a partir dali.
    ///
    /// **Limite:** máximo 5 conexões SSE simultâneas por API key — excedente recebe `429`.
    ///
    /// **Resposta de erro (401 / 429):**
    /// ```json
    /// { "code": "Sse.TooManyConnections", "message": "..." }
    /// ```
    /// </remarks>
    /// <response code="200">Stream SSE aberto.</response>
    /// <response code="401">Header X-Api-Key ausente ou inválido.</response>
    /// <response code="429">Limite de conexões simultâneas excedido.</response>
    [HttpGet("dashboard")]
    [Produces("text/event-stream")]
    public async Task Dashboard(
        [FromServices] CurrentApplication current,
        [FromServices] SseConnectionLimiter limiter,
        [FromServices] IConnectionMultiplexer redis,
        [FromServices] INotificationRepository notifications,
        CancellationToken cancellationToken)
    {
        var appId = current.ApplicationId;

        if (!limiter.TryAcquire(appId))
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await Response.WriteAsJsonAsync(new
            {
                Code = "Sse.TooManyConnections",
                Message = $"Limite de {SseConnectionLimiter.MaxPerApplication} conexões SSE simultâneas por API key."
            }, cancellationToken);
            return;
        }

        var writeLock = new SemaphoreSlim(1, 1);
        var subscriber = redis.GetSubscriber();
        var channel = DashboardEvents.Channel(appId);

        try
        {
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            await Response.Body.FlushAsync(cancellationToken);

            await subscriber.SubscribeAsync(channel, async (_, value) =>
            {
                var raw = (string?)value;
                if (string.IsNullOrEmpty(raw)) return;
                try { await WriteRedisEventAsync(raw, writeLock, cancellationToken); }
                catch { /* connection dropping */ }
            });

            await WriteMetricsAsync(notifications, appId, writeLock, cancellationToken);

            var keepalive = LoopAsync(KeepaliveInterval, () => WriteRawAsync(":keepalive\n\n", writeLock, cancellationToken), cancellationToken);
            var metrics = LoopAsync(MetricsInterval, () => WriteMetricsAsync(notifications, appId, writeLock, cancellationToken), cancellationToken);

            await Task.WhenAny(keepalive, metrics);
        }
        catch (OperationCanceledException) { }
        finally
        {
            try { await subscriber.UnsubscribeAsync(channel); } catch { }
            limiter.Release(appId);
        }
    }

    private static async Task LoopAsync(TimeSpan interval, Func<Task> action, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(interval, ct);
            await action();
        }
    }

    private async Task WriteMetricsAsync(INotificationRepository repo, Guid appId, SemaphoreSlim writeLock, CancellationToken ct)
    {
        var metrics = await repo.GetDashboardSnapshotAsync(appId);
        var json = JsonSerializer.Serialize(metrics, JsonOptions);
        await WriteFrameAsync("metrics.snapshot", json, writeLock, ct);
    }

    private async Task WriteRedisEventAsync(string redisPayload, SemaphoreSlim writeLock, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(redisPayload);
        var type = doc.RootElement.GetProperty("type").GetString();
        if (string.IsNullOrEmpty(type)) return;
        var data = doc.RootElement.GetProperty("data").GetRawText();
        await WriteFrameAsync(type, data, writeLock, ct);
    }

    private async Task WriteFrameAsync(string eventType, string dataJson, SemaphoreSlim writeLock, CancellationToken ct)
        => await WriteRawAsync($"event: {eventType}\ndata: {dataJson}\n\n", writeLock, ct);

    private async Task WriteRawAsync(string frame, SemaphoreSlim writeLock, CancellationToken ct)
    {
        await writeLock.WaitAsync(ct);
        try
        {
            await Response.WriteAsync(frame, ct);
            await Response.Body.FlushAsync(ct);
        }
        finally { writeLock.Release(); }
    }
}
