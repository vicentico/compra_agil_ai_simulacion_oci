using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ppip.BuildingBlocks.Observability;

/// <summary>
/// Implementa la convención de correlación de docs/06-api/00-api-conventions.md:
/// acepta <c>X-Correlation-Id</c> entrante (o lo genera), lo devuelve siempre en
/// la respuesta, lo agrega como tag del span activo (W3C traceparent) y lo
/// inyecta en el scope de logging para que todo log estructurado lo incluya.
/// El header de respuesta se fija antes de llamar a `next` (no vía
/// `OnStarting`): el valor ya se conoce en ese punto y nada aguas abajo
/// depende de que la respuesta haya comenzado.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString() : incoming;

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        Activity.Current?.SetTag("correlation_id", correlationId);

        using (logger.BeginScope(new Dictionary<string, object> { ["correlationId"] = correlationId }))
        {
            await next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
