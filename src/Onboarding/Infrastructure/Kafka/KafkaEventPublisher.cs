using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Onboarding.Application.Abstractions;

namespace Onboarding.Infrastructure.Kafka;

public sealed class KafkaEventPublisher(IProducer<string, string> producer) : IEventPublisher
{
    public const string ActivitySourceName = "Onboarding.Kafka";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("kafka publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", message.Topic);
        activity?.SetTag("messaging.message.id", message.EventId);
        activity?.SetTag("correlation.id", message.CorrelationId);

        var envelope = new
        {
            message.EventId,
            message.EventType,
            message.AggregateId,
            message.AggregateType,
            message.OccurredAt,
            message.CorrelationId,
            message.Version,
            Payload = JsonSerializer.Deserialize<JsonElement>(message.Payload)
        };

        var delivery = await producer.ProduceAsync(message.Topic, new Message<string, string>
        {
            Key = message.AggregateId,
            Value = JsonSerializer.Serialize(envelope, JsonOptions),
            Headers = new Headers
            {
                { "correlation-id", System.Text.Encoding.UTF8.GetBytes(message.CorrelationId) },
                { "event-id", System.Text.Encoding.UTF8.GetBytes(message.EventId) }
            }
        }, cancellationToken);

        if (delivery.Status != PersistenceStatus.Persisted)
        {
            throw new InvalidOperationException(
                $"Kafka did not persist event {message.EventId}. Status: {delivery.Status}.");
        }
    }
}
