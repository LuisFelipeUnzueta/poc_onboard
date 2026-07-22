using Mapster;
using Onboarding.Extensions;
using Onboarding.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);
TypeAdapterConfig.GlobalSettings.Compile();

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddLifeCycle(builder.Configuration);
builder.Services.AddPhaseFour(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

app.Run();

public partial class Program;
