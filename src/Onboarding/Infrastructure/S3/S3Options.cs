namespace Onboarding.Infrastructure.S3;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string BucketName { get; init; } = "onboarding-documents";
    public string ServiceUrl { get; init; } = "http://localhost:4566";
    public string Region { get; init; } = "us-east-1";
    public string AccessKey { get; init; } = "local";
    public string SecretKey { get; init; } = "local";
}
