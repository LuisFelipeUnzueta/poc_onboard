using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Onboarding.Application.Abstractions;
using Onboarding.Application.Proposals;
using Onboarding.Infrastructure.DynamoDb;
using Onboarding.Infrastructure.Redis;
using StackExchange.Redis;

namespace Onboarding.Extensions;

public static class LifeCycleStartupExtension
{
    public static IServiceCollection AddLifeCycle(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DynamoDbOptions>(configuration.GetSection(DynamoDbOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            var options = configuration.GetSection(DynamoDbOptions.SectionName).Get<DynamoDbOptions>() ?? new DynamoDbOptions();
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = options.ServiceUrl,
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            return new AmazonDynamoDBClient(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        services.AddScoped<IProposalRepository, DynamoDbProposalRepository>();
        services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();
        services.AddScoped<IDocumentRulesService, DocumentRulesService>();
        services.AddScoped<ICreateProposalUseCase, CreateProposalUseCase>();
        services.AddScoped<IGetProposalUseCase, GetProposalUseCase>();

        return services;
    }
}
