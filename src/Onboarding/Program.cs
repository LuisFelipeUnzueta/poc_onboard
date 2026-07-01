using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Onboarding.Application.Abstractions;
using Onboarding.Application.Proposals;
using Onboarding.Infrastructure.DynamoDb;
using Onboarding.Infrastructure.Redis;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<DynamoDbOptions>(builder.Configuration.GetSection(DynamoDbOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var options = builder.Configuration.GetSection(DynamoDbOptions.SectionName).Get<DynamoDbOptions>() ?? new DynamoDbOptions();
    var config = new AmazonDynamoDBConfig
    {
        ServiceURL = options.ServiceUrl,
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
    };

    return new AmazonDynamoDBClient(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
    return ConnectionMultiplexer.Connect(options.ConnectionString);
});

builder.Services.AddScoped<IProposalRepository, DynamoDbProposalRepository>();
builder.Services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();
builder.Services.AddScoped<IDocumentRulesService, DocumentRulesService>();
builder.Services.AddScoped<ICreateProposalUseCase, CreateProposalUseCase>();
builder.Services.AddScoped<IGetProposalUseCase, GetProposalUseCase>();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok("Alive"));
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

app.Run();

public partial class Program;
