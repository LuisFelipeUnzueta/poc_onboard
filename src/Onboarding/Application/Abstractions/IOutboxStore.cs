namespace Onboarding.Application.Abstractions;

public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int limit, CancellationToken cancellationToken);
    Task MarkPublishedAsync(string eventId, DateTimeOffset publishedAt, CancellationToken cancellationToken);
}

public sealed record OutboxMessage(
    string EventId,
    string EventType,
    string AggregateId,
    string AggregateType,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    int Version,
    string Topic,
    string Payload);
