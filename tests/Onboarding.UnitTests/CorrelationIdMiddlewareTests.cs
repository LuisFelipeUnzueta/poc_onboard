using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Onboarding.Infrastructure.Observability;
using Onboarding.Middleware;

namespace Onboarding.UnitTests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_Generate_CorrelationId_When_Header_Is_Absent()
    {
        var context = new DefaultHttpContext();
        var correlationContext = new CorrelationContext();
        string? correlationIdDuringRequest = null;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            correlationIdDuringRequest = correlationContext.CorrelationId;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, correlationContext);

        correlationIdDuringRequest.Should().NotBeNullOrWhiteSpace();
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(correlationIdDuringRequest);
        context.Items[CorrelationIdMiddleware.ItemName].Should().Be(correlationIdDuringRequest);
    }

    [Fact]
    public async Task InvokeAsync_Should_Preserve_CorrelationId_When_Header_Is_Present()
    {
        const string expectedCorrelationId = "client-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = expectedCorrelationId;
        var correlationContext = new CorrelationContext();
        string? correlationIdDuringRequest = null;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            correlationIdDuringRequest = correlationContext.CorrelationId;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, correlationContext);

        correlationIdDuringRequest.Should().Be(expectedCorrelationId);
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(expectedCorrelationId);
    }
}
