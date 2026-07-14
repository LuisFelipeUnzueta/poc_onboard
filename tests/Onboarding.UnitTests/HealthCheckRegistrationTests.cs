using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Onboarding.Extensions;

namespace Onboarding.UnitTests;

public sealed class HealthCheckRegistrationTests
{
    [Fact]
    public void AddPhaseFour_Should_Register_Readiness_Dependencies()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:BootstrapServers"] = "localhost:9092"
            })
            .Build();
        using var services = new ServiceCollection()
            .AddLogging()
            .AddPhaseFour(configuration)
            .BuildServiceProvider();

        var registrations = services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value.Registrations;

        registrations.Where(registration => registration.Tags.Contains("ready"))
            .Select(registration => registration.Name)
            .Should().BeEquivalentTo("dynamodb", "redis", "kafka");
    }
}
