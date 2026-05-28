using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using notX.Application.Interfaces;
using notX.Application.Interfaces.Repositories;
using notX.Domain.Enums;
using StackExchange.Redis;

namespace notX.EmailWorker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IConnectionMultiplexer redis,
    ILogger<Worker> logger) : BackgroundService
{
    private const string LockKey = "notx:outbox:lock";
    private static readonly TimeSpan LockTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 50;
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unexpected error in worker loop");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        var lockToken = Guid.NewGuid();
        var db = redis.GetDatabase();

        var acquired = await db.StringSetAsync(
            LockKey,
            lockToken.ToString(),
            LockTtl,
            When.NotExists);

        if (!acquired)
        {
            logger.LogDebug("Could not acquire Redis lock — another worker is processing");
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var messages = (await outboxRepo.ClaimBatchAsync(lockToken, BatchSize, stoppingToken)).ToList();

            if (messages.Count == 0)
            {
                logger.LogDebug("No pending outbox messages");
                return;
            }

            logger.LogInformation("Processing {Count} outbox messages", messages.Count);

            foreach (var message in messages)
            {
                if (stoppingToken.IsCancellationRequested) break;

                await ProcessMessageAsync(message, outboxRepo, notificationRepo, emailService, stoppingToken);
            }
        }
        finally
        {
            var currentValue = await db.StringGetAsync(LockKey);
            if (currentValue == lockToken.ToString())
                await db.KeyDeleteAsync(LockKey);
        }
    }

    private async Task ProcessMessageAsync(
        Domain.Entities.OutboxMessage message,
        IOutboxRepository outboxRepo,
        INotificationRepository notificationRepo,
        IEmailService emailService,
        CancellationToken stoppingToken)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<OutboxPayload>(message.Payload);
            if (payload is null)
            {
                logger.LogWarning("Invalid outbox payload for message {Id}", message.Id);
                await outboxRepo.MarkProcessedAsync(message.Id, stoppingToken);
                return;
            }

            var notification = await notificationRepo.GetByIdAsync(payload.NotificationId);
            if (notification is null)
            {
                logger.LogWarning("Notification {Id} not found for outbox message {OutboxId}",
                    payload.NotificationId, message.Id);
                await outboxRepo.MarkProcessedAsync(message.Id, stoppingToken);
                return;
            }

            notification.MarkAsProcessing();
            await notificationRepo.UpdateStatusAsync(notification.Id, NotificationStatus.Processing);

            if (notification.Type == NotificationType.Email)
            {
                var result = await emailService.SendAsync(
                    notification.Recipient,
                    notification.Title,
                    notification.Content,
                    stoppingToken);

                if (result.IsSuccess)
                {
                    notification.MarkAsSent();
                    await notificationRepo.UpdateStatusAsync(notification.Id, NotificationStatus.Sent, notification.SentAt);
                    await outboxRepo.MarkProcessedAsync(message.Id, stoppingToken);
                    logger.LogInformation("Notification {Id} sent successfully", notification.Id);
                }
                else
                {
                    await HandleFailureAsync(message, notification, result.Error.Message, outboxRepo, notificationRepo, stoppingToken);
                }
            }
            else
            {
                logger.LogWarning("Notification type {Type} not supported by EmailWorker — skipping", notification.Type);
                await outboxRepo.MarkProcessedAsync(message.Id, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
            await HandleFailureAsync(message, null, ex.Message, outboxRepo, notificationRepo, stoppingToken);
        }
    }

    private async Task HandleFailureAsync(
        Domain.Entities.OutboxMessage message,
        Domain.Entities.Notification? notification,
        string error,
        IOutboxRepository outboxRepo,
        INotificationRepository notificationRepo,
        CancellationToken stoppingToken)
    {
        var newRetryCount = message.RetryCount + 1;
        await outboxRepo.IncrementRetryAsync(message.Id, error, stoppingToken);

        if (newRetryCount >= MaxRetries)
        {
            await outboxRepo.MarkProcessedAsync(message.Id, stoppingToken);

            if (notification is not null)
            {
                notification.MarkAsFailed();
                await notificationRepo.UpdateStatusAsync(notification.Id, NotificationStatus.Failed);
            }

            logger.LogWarning("Outbox message {OutboxId} esgotou {Retries} tentativas — marcado como finalizado",
                message.Id, newRetryCount);
        }
        else
        {
            logger.LogWarning("Outbox message {Id} falhou (tentativa {Attempt}/{Max}): {Error}",
                message.Id, newRetryCount, MaxRetries, error);
        }
    }

    private sealed record OutboxPayload(
        [property: System.Text.Json.Serialization.JsonPropertyName("notificationId")] Guid NotificationId,
        [property: System.Text.Json.Serialization.JsonPropertyName("type")] string Type);
}
