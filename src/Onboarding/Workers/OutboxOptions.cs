namespace Onboarding.Workers;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; init; } = 25;
    public int PollingIntervalSeconds { get; init; } = 2;
}
