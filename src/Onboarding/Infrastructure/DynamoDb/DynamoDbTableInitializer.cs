using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;

namespace Onboarding.Infrastructure.DynamoDb;

public sealed class DynamoDbTableInitializer(
    IAmazonDynamoDB dynamoDb,
    IOptions<DynamoDbOptions> options,
    ILogger<DynamoDbTableInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.InitializeTable)
        {
            return;
        }

        try
        {
            var table = await dynamoDb.DescribeTableAsync(options.Value.TableName, cancellationToken);
            if (table.Table.GlobalSecondaryIndexes.All(index => index.IndexName != "OutboxIndex"))
            {
                await AddOutboxIndexAsync(cancellationToken);
            }

            return;
        }
        catch (ResourceNotFoundException)
        {
        }

        await dynamoDb.CreateTableAsync(new CreateTableRequest
        {
            TableName = options.Value.TableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions =
            [
                new("PK", ScalarAttributeType.S),
                new("SK", ScalarAttributeType.S),
                new("GSI1PK", ScalarAttributeType.S),
                new("GSI1SK", ScalarAttributeType.S),
                new("GSI2PK", ScalarAttributeType.S),
                new("GSI2SK", ScalarAttributeType.S),
                new("GSI3PK", ScalarAttributeType.S),
                new("GSI3SK", ScalarAttributeType.S)
            ],
            KeySchema = [new("PK", KeyType.HASH), new("SK", KeyType.RANGE)],
            GlobalSecondaryIndexes =
            [
                CreateIndex("StatusIndex", "GSI1PK", "GSI1SK"),
                CreateIndex("CnpjIndex", "GSI2PK", "GSI2SK"),
                CreateIndex("OutboxIndex", "GSI3PK", "GSI3SK")
            ]
        }, cancellationToken);

        logger.LogInformation("DynamoDB table {TableName} created", options.Value.TableName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static GlobalSecondaryIndex CreateIndex(string name, string partitionKey, string sortKey) => new()
    {
        IndexName = name,
        KeySchema = [new(partitionKey, KeyType.HASH), new(sortKey, KeyType.RANGE)],
        Projection = new Projection { ProjectionType = ProjectionType.ALL }
    };

    private async Task AddOutboxIndexAsync(CancellationToken cancellationToken)
    {
        await dynamoDb.UpdateTableAsync(new UpdateTableRequest
        {
            TableName = options.Value.TableName,
            AttributeDefinitions =
            [
                new("GSI3PK", ScalarAttributeType.S),
                new("GSI3SK", ScalarAttributeType.S)
            ],
            GlobalSecondaryIndexUpdates =
            [
                new()
                {
                    Create = new CreateGlobalSecondaryIndexAction
                    {
                        IndexName = "OutboxIndex",
                        KeySchema = [new("GSI3PK", KeyType.HASH), new("GSI3SK", KeyType.RANGE)],
                        Projection = new Projection { ProjectionType = ProjectionType.ALL }
                    }
                }
            ]
        }, cancellationToken);

        logger.LogInformation("DynamoDB index OutboxIndex created on table {TableName}", options.Value.TableName);
    }
}
