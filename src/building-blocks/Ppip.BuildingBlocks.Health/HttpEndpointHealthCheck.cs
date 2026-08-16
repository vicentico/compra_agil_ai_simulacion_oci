using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ppip.BuildingBlocks.Health;

/// <summary>
/// Verifica que un endpoint HTTP dependiente responda. Se usa para
/// dependencias sin SDK propio en esta fase (MinIO, Qdrant, Ollama) — ver
/// docs/09-document-intelligence, docs/10-rag, ADR-006/007. El SDK dedicado
/// se incorpora recién cuando se implemente la lógica real (FASE 7+).
/// </summary>
public sealed class HttpEndpointHealthCheck(IHttpClientFactory httpClientFactory, string name, string url) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient(nameof(HttpEndpointHealthCheck));
            client.Timeout = TimeSpan.FromSeconds(3);
            var response = await client.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"{name} respondió {(int)response.StatusCode}.")
                : HealthCheckResult.Degraded($"{name} respondió {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            // Degraded, no Unhealthy: dependencias externas no deben tumbar
            // /ready del todo (NFR-006, docs/14-reliability). El caller decide
            // si esta dependencia es crítica o degradable para su servicio.
            return HealthCheckResult.Degraded($"{name} no respondió.", ex);
        }
    }
}
