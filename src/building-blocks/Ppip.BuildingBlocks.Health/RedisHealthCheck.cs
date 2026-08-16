using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Ppip.BuildingBlocks.Health;

/// <summary>
/// Verifica conectividad real con Redis (cache/locks/dedupe — ver
/// docs/08-data/01-data-architecture.md). Se usa en /ready.
/// </summary>
public sealed class RedisHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.ConnectTimeout = 3000;
            options.AbortOnConnectFail = true;
            await using var connection = await ConnectionMultiplexer.ConnectAsync(options);
            var pong = await connection.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy($"Redis responde en {pong.TotalMilliseconds}ms.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No se pudo conectar a Redis.", ex);
        }
    }
}
