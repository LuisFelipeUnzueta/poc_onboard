using Onboarding.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddLifeCycle(builder.Configuration);

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok("Alive"));
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

app.Run();

public partial class Program;
