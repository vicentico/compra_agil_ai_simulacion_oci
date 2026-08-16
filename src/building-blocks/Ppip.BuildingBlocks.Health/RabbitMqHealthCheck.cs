using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Ppip.BuildingBlocks.Health;

/// <summary>
/// Verifica conectividad real con RabbitMQ abriendo y cerrando una conexión
/// AMQP de corta duración. Se usa en /ready — ver ADR-003.
/// </summary>
public sealed class RabbitMqHealthCheck(string host, string username, string password) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = username,
                Password = password,
            };
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy("RabbitMQ acepta conexiones AMQP.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No se pudo conectar a RabbitMQ.", ex);
        }
    }
}
