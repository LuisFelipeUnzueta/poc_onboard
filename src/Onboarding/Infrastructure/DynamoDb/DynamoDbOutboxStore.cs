using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Onboarding.Application.Abstractions;

namespace Onboarding.Infrastructure.DynamoDb;

public sealed class DynamoDbOutboxStore(IAmazonDynamoDB dynamoDb, IOptions<DynamoDbOptions> options) : IOutboxStore
{
    private readonly string _tableName = options.Value.TableName;

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int limit, CancellationToken cancellationToken)
    {
        var response = await dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "OutboxIndex",
            KeyConditionExpression = "GSI3PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":pk"] = new("OUTBOX#PENDING") },
            Limit = limit,
            ScanIndexForward = true
        }, cancellationToken);

        return response.Items.Select(ToMessage).ToArray();
    }

    public async Task MarkPublishedAsync(string eventId, DateTimeOffset publishedAt, CancellationToken cancellationToken)
    {
        await dynamoDb.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new($"OUTBOX#{eventId}"),
                ["SK"] = new("#METADATA")
            },
            UpdateExpression = "SET #status = :published, PublishedAt = :publishedAt REMOVE GSI3PK, GSI3SK",
            ConditionExpression = "#status = :pending",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#status"] = "Status" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pending"] = new("PENDING"),
                [":published"] = new("PUBLISHED"),
                [":publishedAt"] = new(publishedAt.ToString("O"))
            }
        }, cancellationToken);
    }

    private static OutboxMessage ToMessage(Dictionary<string, AttributeValue> item) => new(
        item["EventId"].S,
        item["EventType"].S,
        item["AggregateId"].S,
        item["AggregateType"].S,
        DateTimeOffset.Parse(item["OccurredAt"].S, CultureInfo.InvariantCulture),
        item["CorrelationId"].S,
        int.Parse(item["Version"].N, CultureInfo.InvariantCulture),
        item["Topic"].S,
        item["Payload"].S);
}
