using Microsoft.AspNetCore.Http;

namespace Ppip.BuildingBlocks.Observability;

/// <summary>
/// Propaga el <c>X-Correlation-Id</c> del request entrante hacia llamadas HTTP
/// salientes (HttpClient), para que la correlación sobreviva saltos entre
/// servicios (docs/06-api/00-api-conventions.md — "propagado a eventos y logs").
/// Requiere <c>AddHttpContextAccessor()</c> registrado.
/// </summary>
public sealed class CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.HeaderName] is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Remove(CorrelationIdMiddleware.HeaderName);
            request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
