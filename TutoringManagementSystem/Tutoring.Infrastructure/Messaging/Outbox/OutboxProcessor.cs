using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tutoring.Infrastructure.FileStorage;
using Tutoring.Infrastructure.Messaging.Contracts;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Infrastructure.Messaging.Outbox;

public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IPublisher publisher,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger,
    IOptions<OutboxOptions> options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessMessagesAsync(stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(options.Value.ProcessingIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task ProcessMessagesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<TutoringDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null)
            .OrderBy(x => x.RetryCount)
            .ThenBy(x => x.OccurredAtUtc)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                if (message.Type == LearningMaterialCleanup.MessageType)
                {
                    var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
                    await LearningMaterialCleanup.RemoveAsync(dbContext, storage, message.Payload, cancellationToken);
                }
                else
                {
                    await publisher.PublishAsync(message.Id, message.Type, message.Payload, cancellationToken);
                }

                message.ProcessedAtUtc =
                    timeProvider.GetUtcNow();

                message.Error = null;

                logger.LogInformation(
                    "Outbox message processed. MessageId: {MessageId}",
                    message.Id);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                message.RetryCount++;

                message.Error = exception.Message;

                logger.LogError(
                    exception,
                    "Failed to process outbox message. MessageId: {MessageId}",
                    message.Id);
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (message.Type == LearningMaterialCleanup.MessageType)
            {
                // Another worker may have removed the same material. Retry from a fresh context.
                return;
            }
        }
    }
}
