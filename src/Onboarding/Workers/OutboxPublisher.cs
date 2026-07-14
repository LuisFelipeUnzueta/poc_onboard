using Microsoft.Extensions.Options;
using Onboarding.Application.Abstractions;

namespace Onboarding.Workers;

public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds));

        do
        {
            await PublishPendingAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            var messages = await outboxStore.GetPendingAsync(options.Value.BatchSize, cancellationToken);

            foreach (var message in messages)
            {
                await eventPublisher.PublishAsync(message, cancellationToken);
                await outboxStore.MarkPublishedAsync(message.EventId, DateTimeOffset.UtcNow, cancellationToken);
                logger.LogInformation(
                    "Outbox event {EventId} published to {Topic} for proposal {ProposalId}",
                    message.EventId,
                    message.Topic,
                    message.AggregateId);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish pending outbox events");
        }
    }
}
