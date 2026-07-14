namespace Onboarding.Infrastructure.DynamoDb;

public sealed class DynamoDbOptions
{
    public const string SectionName = "DynamoDb";

    public string TableName { get; init; } = "Onboarding";
    public string ServiceUrl { get; init; } = "http://localhost:8000";
    public string Region { get; init; } = "us-east-1";
    public string AccessKey { get; init; } = "local";
    public string SecretKey { get; init; } = "local";
    public bool InitializeTable { get; init; }
}
