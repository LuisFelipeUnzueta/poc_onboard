using System.Diagnostics;
using Onboarding.Application.Abstractions;
using Serilog.Context;

namespace Onboarding.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemName = "CorrelationId";

    public async Task InvokeAsync(HttpContext context, ICorrelationContext correlationContext)
    {
        var previousCorrelationId = correlationContext.CorrelationId;
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var header)
            && !string.IsNullOrWhiteSpace(header)
                ? header.ToString()
                : Guid.CreateVersion7().ToString();

        correlationContext.CorrelationId = correlationId;
        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);
        Activity.Current?.AddBaggage("correlation.id", correlationId);

        using (LogContext.PushProperty(ItemName, correlationId))
        {
            try
            {
                await next(context);
            }
            finally
            {
                correlationContext.CorrelationId = previousCorrelationId;
            }
        }
    }
}
