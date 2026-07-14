using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.Runtime;
using Onboarding.Application.Abstractions;
using Onboarding.Application.Proposals;
using Onboarding.Infrastructure.DynamoDb;
using Onboarding.Infrastructure.Redis;
using Onboarding.Infrastructure.S3;
using StackExchange.Redis;

namespace Onboarding.Extensions;

public static class LifeCycleStartupExtension
{
    public static IServiceCollection AddLifeCycle(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DynamoDbOptions>(configuration.GetSection(DynamoDbOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            var options = configuration.GetSection(DynamoDbOptions.SectionName).Get<DynamoDbOptions>() ?? new DynamoDbOptions();
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = options.ServiceUrl,
                AuthenticationRegion = options.Region
            };

            return new AmazonDynamoDBClient(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var options = configuration.GetSection(S3Options.SectionName).Get<S3Options>() ?? new S3Options();
            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                AuthenticationRegion = options.Region,
                ForcePathStyle = true
            };

            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddScoped<IProposalRepository, DynamoDbProposalRepository>();
        services.AddScoped<IDocumentStorage, S3DocumentStorage>();
        services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();
        services.AddScoped<IDocumentRulesService, DocumentRulesService>();
        services.AddScoped<ICreateProposalUseCase, CreateProposalUseCase>();
        services.AddScoped<IGetProposalUseCase, GetProposalUseCase>();
        services.AddScoped<IUploadDocumentUseCase, UploadDocumentUseCase>();
        services.AddHostedService<DynamoDbTableInitializer>();

        return services;
    }
}
