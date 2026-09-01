using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            .OrderBy(x => x.OccurredAtUtc)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(
                    message.Id,
                    message.Type,
                    message.Payload,
                    cancellationToken);

                message.ProcessedAtUtc =
                    timeProvider.GetUtcNow();

                message.Error = null;

                logger.LogInformation(
                    "Outbox message published. MessageId: {MessageId}",
                    message.Id);
            }
            catch (Exception exception)
            {
                message.RetryCount++;

                message.Error = exception.Message;

                logger.LogError(
                    exception,
                    "Failed to publish outbox message. MessageId: {MessageId}",
                    message.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}