using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Onboarding.Application.Abstractions;
using Onboarding.Infrastructure.DynamoDb;
using Onboarding.Infrastructure.Kafka;
using Onboarding.Infrastructure.Observability;
using Onboarding.Workers;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Onboarding.Extensions;

public static class PhaseFourStartupExtension
{
    public static IServiceCollection AddPhaseFour(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddSingleton(serviceProvider =>
        {
            var kafkaOptions = serviceProvider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            return new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
                ClientId = kafkaOptions.ClientId,
                Acks = Acks.All,
                EnableIdempotence = true
            }).Build();
        });
        services.AddSingleton<IAdminClient>(serviceProvider =>
        {
            var kafkaOptions = serviceProvider.GetRequiredService<IOptions<KafkaOptions>>().Value;
            return new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = kafkaOptions.BootstrapServers,
                ClientId = $"{kafkaOptions.ClientId}-health"
            }).Build();
        });

        services.AddSingleton<ICorrelationContext, CorrelationContext>();
        services.AddSingleton<IOnboardingMetrics, OnboardingMetrics>();
        services.AddScoped<IOutboxStore, DynamoDbOutboxStore>();
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddHostedService<OutboxPublisher>();

        services.AddHealthChecks()
            .AddCheck<DynamoDbHealthCheck>("dynamodb", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
            .AddCheck<KafkaHealthCheck>("kafka", tags: ["ready"]);

        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("Onboarding"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(KafkaEventPublisher.ActivitySourceName);

                if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(OnboardingMetrics.MeterName);

                if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = endpoint);
                }
            });

        return services;
    }
}
