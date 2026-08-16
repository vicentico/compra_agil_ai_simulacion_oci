using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ppip.BuildingBlocks.Observability.Tests;

/// <summary>
/// Cubre la convención de correlación de docs/06-api/00-api-conventions.md:
/// "se acepta X-Correlation-Id entrante (se genera si falta) y siempre se
/// devuelve en la respuesta".
/// </summary>
public class CorrelationIdMiddlewareTests
{
    private static CorrelationIdMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<CorrelationIdMiddleware>.Instance);

    [Fact]
    public async Task GeneratesCorrelationId_WhenHeaderMissing()
    {
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items[CorrelationIdMiddleware.HeaderName]);
        Assert.True(Guid.TryParse(correlationId, out _));
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task PreservesIncomingCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "test-correlation-123";
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("test-correlation-123", context.Items[CorrelationIdMiddleware.HeaderName]);
        Assert.Equal("test-correlation-123", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task IgnoresBlankIncomingHeader_AndGeneratesOne()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "   ";
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var correlationId = Assert.IsType<string>(context.Items[CorrelationIdMiddleware.HeaderName]);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task CallsNext_ExactlyOnce()
    {
        var context = new DefaultHttpContext();
        var callCount = 0;
        var middleware = CreateMiddleware(_ =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(1, callCount);
    }
}
