using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ppip.BuildingBlocks.Health;

/// <summary>
/// Verifica conectividad real con MongoDB ejecutando "ping" contra la base
/// "admin". Se usa en /ready (no en /health de liveness) — ver
/// docs/13-observability/01-observability-spec.md.
/// </summary>
public sealed class MongoPingHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new MongoClient(connectionString);
            var admin = client.GetDatabase("admin");
            await admin.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB responde a ping.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No se pudo conectar a MongoDB.", ex);
        }
    }
}
