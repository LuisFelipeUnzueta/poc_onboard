using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.Kafka;

namespace Onboarding.IntegrationTests;

[Collection(InfrastructureCollection.Name)]
public sealed class MessagingAndHealthTests(InfrastructureFixture fixture)
{
    [Fact]
    public async Task Health_Endpoints_Should_Reflect_Configured_Dependencies()
    {
        using var client = fixture.Factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        var ready = await client.GetAsync("/health/ready");

        live.StatusCode.Should().Be(HttpStatusCode.OK);
        ready.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProposal_Should_Persist_Publish_And_Mark_Outbox_Event()
    {
        const string correlationId = "integration-correlation-id";
        fixture.Factory.Services.GetRequiredService<IOptions<KafkaOptions>>().Value.BootstrapServers
            .Should().Be(fixture.KafkaBootstrapServers);
        using var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.PostAsJsonAsync("/api/proposals", CreateProposalRequest());
        var responseContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, responseContent);
        using var responseBody = JsonDocument.Parse(responseContent);
        var proposalId = responseBody.RootElement.GetProperty("proposalId").GetString();

        var createdOutboxItem = await WaitForOutboxAsync(proposalId!);
        createdOutboxItem["Topic"].S.Should().Be("merchant.proposal.created");
        var eventId = createdOutboxItem["EventId"].S;
        var publishedOutboxItem = await WaitForPublishedOutboxAsync(eventId);
        publishedOutboxItem["Status"].S.Should().Be("PUBLISHED");
        publishedOutboxItem["PublishedAt"].S.Should().NotBeNullOrWhiteSpace();

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = fixture.KafkaBootstrapServers,
            GroupId = $"onboarding-tests-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = fixture.KafkaBootstrapServers
        }).Build();
        var topicMetadata = adminClient.GetMetadata("merchant.proposal.created", TimeSpan.FromSeconds(10));
        var partitions = topicMetadata.Topics.Single().Partitions.Select(partition =>
            new TopicPartitionOffset(
                "merchant.proposal.created",
                new Partition(partition.PartitionId),
                Offset.Beginning)).ToArray();
        consumer.Assign(partitions);
        var availableMessages = partitions.Sum(partition => consumer.QueryWatermarkOffsets(
            partition.TopicPartition,
            TimeSpan.FromSeconds(10)).High.Value);
        availableMessages.Should().BeGreaterThan(0, "the outbox publisher received a Kafka delivery acknowledgement");
        var consumed = consumer.Consume(TimeSpan.FromSeconds(20));
        consumed.Should().NotBeNull();
        consumed!.Message.Key.Should().Be(proposalId);
        using var envelope = JsonDocument.Parse(consumed.Message.Value);
        var root = envelope.RootElement;
        root.GetProperty("eventType").GetString().Should().Be("ProposalCreated");
        root.GetProperty("aggregateId").GetString().Should().Be(proposalId);
        root.GetProperty("correlationId").GetString().Should().Be(correlationId);

        root.GetProperty("eventId").GetString().Should().Be(eventId);
    }

    private async Task<Dictionary<string, AttributeValue>> WaitForOutboxAsync(string proposalId)
    {
        var dynamoDb = fixture.Factory.Services.GetRequiredService<IAmazonDynamoDB>();

        for (var attempt = 0; attempt < 40; attempt++)
        {
            var response = await dynamoDb.ScanAsync(new ScanRequest
            {
                TableName = "OnboardingIntegrationTests",
                FilterExpression = "#type = :type AND AggregateId = :aggregateId",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#type"] = "Type" },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":type"] = new("Outbox"),
                    [":aggregateId"] = new(proposalId)
                }
            });

            if (response.Items.Count > 0)
            {
                return response.Items[0];
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Outbox event for proposal {proposalId} was not persisted.");
    }

    private async Task<Dictionary<string, AttributeValue>> WaitForPublishedOutboxAsync(string eventId)
    {
        var dynamoDb = fixture.Factory.Services.GetRequiredService<IAmazonDynamoDB>();

        for (var attempt = 0; attempt < 40; attempt++)
        {
            var response = await dynamoDb.GetItemAsync(new GetItemRequest
            {
                TableName = "OnboardingIntegrationTests",
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new($"OUTBOX#{eventId}"),
                    ["SK"] = new("#METADATA")
                },
                ConsistentRead = true
            });

            if (response.Item.TryGetValue("Status", out var status) && status.S == "PUBLISHED")
            {
                return response.Item;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Outbox event {eventId} was not marked as published.");
    }

    private static object CreateProposalRequest() => new
    {
        partnerId = PartnerId.New().Value,
        cnpj = "11.444.777/0001-61",
        legalName = "Empresa Integracao Ltda",
        segment = "PayFac",
        mcc = "5411",
        partners = new[]
        {
            new
            {
                name = "Joao Silva",
                cpf = "529.982.247-25",
                participationPercentage = 100,
                isLegalRepresentative = true
            }
        },
        bankAccount = new
        {
            ispb = "60746948",
            agency = "0001",
            accountNumber = "123456",
            accountDigit = "7",
            accountType = "CheckingAccount"
        },
        address = new
        {
            zipCode = "01310-100",
            street = "Av. Paulista",
            number = "1000",
            complement = (string?)null,
            neighborhood = "Bela Vista",
            city = "Sao Paulo",
            state = "SP"
        }
    };
}
