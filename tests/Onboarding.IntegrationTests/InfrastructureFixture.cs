using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Onboarding.IntegrationTests;

public sealed class InfrastructureFixture : IAsyncLifetime
{
    private const ushort DynamoDbPort = 8000;
    private const ushort RedisPort = 6379;
    private const ushort KafkaPort = 19092;
    private const ushort ZookeeperPort = 22181;
    private readonly IContainer _dynamoDb = new ContainerBuilder("amazon/dynamodb-local:latest")
        .WithPortBinding(DynamoDbPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(DynamoDbPort))
        .Build();
    private readonly IContainer _redis = new ContainerBuilder("redis:7-alpine")
        .WithPortBinding(RedisPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RedisPort))
        .Build();
    private readonly IContainer _zookeeper = new ContainerBuilder("confluentinc/cp-zookeeper:7.3.0")
        .WithPortBinding(ZookeeperPort, 2181)
        .WithEnvironment("ZOOKEEPER_CLIENT_PORT", "2181")
        .WithEnvironment("ZOOKEEPER_TICK_TIME", "2000")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(2181))
        .Build();
    private readonly IContainer _kafka = new ContainerBuilder("confluentinc/cp-kafka:7.3.0")
        .WithPortBinding(KafkaPort, 9092)
        .WithEnvironment("KAFKA_BROKER_ID", "1")
        .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", $"host.docker.internal:{ZookeeperPort}")
        .WithEnvironment("KAFKA_LISTENERS", "INTERNAL://0.0.0.0:29092,EXTERNAL://0.0.0.0:9092")
        .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", $"INTERNAL://localhost:29092,EXTERNAL://localhost:{KafkaPort}")
        .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "INTERNAL:PLAINTEXT,EXTERNAL:PLAINTEXT")
        .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "INTERNAL")
        .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9092))
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string KafkaBootstrapServers => $"localhost:{KafkaPort}";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_dynamoDb.StartAsync(), _redis.StartAsync(), _zookeeper.StartAsync());
        await _kafka.StartAsync();

        Factory = new IntegrationWebApplicationFactory(new Dictionary<string, string?>
        {
            ["DynamoDb:TableName"] = "OnboardingIntegrationTests",
            ["DynamoDb:ServiceUrl"] = $"http://{_dynamoDb.Hostname}:{_dynamoDb.GetMappedPublicPort(DynamoDbPort)}",
            ["DynamoDb:Region"] = "us-east-1",
            ["DynamoDb:AccessKey"] = "local",
            ["DynamoDb:SecretKey"] = "local",
            ["DynamoDb:InitializeTable"] = "true",
            ["Redis:ConnectionString"] = $"{_redis.Hostname}:{_redis.GetMappedPublicPort(RedisPort)}",
            ["Kafka:BootstrapServers"] = KafkaBootstrapServers,
            ["Kafka:ClientId"] = "onboarding-integration-tests",
            ["Outbox:PollingIntervalSeconds"] = "1",
            ["OpenTelemetry:OtlpEndpoint"] = string.Empty
        });
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();
        await _kafka.DisposeAsync();
        await _zookeeper.DisposeAsync();
        await _redis.DisposeAsync();
        await _dynamoDb.DisposeAsync();
    }

    private sealed class IntegrationWebApplicationFactory(IReadOnlyDictionary<string, string?> settings)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Integration");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
        }
    }
}

[CollectionDefinition(Name)]
public sealed class InfrastructureCollection : ICollectionFixture<InfrastructureFixture>
{
    public const string Name = "Infrastructure";
}
