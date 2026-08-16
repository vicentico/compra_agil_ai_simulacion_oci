namespace Ppip.SyncWorker;

/// <summary>
/// Placeholder de FASE 1: solo demuestra que el worker corre como
/// BackgroundService dentro de un host con endpoint HTTP de salud. La lógica
/// real (UC-001: ChileCompraClient, SyncOrchestrator, CheckpointStore) se
/// implementa en FASE 5-6 — ver docs/02-use-cases/UC-001-sincronizar-compras-agiles.md.
/// </summary>
public sealed class HeartbeatWorker(ILogger<HeartbeatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("sync-worker heartbeat (esqueleto FASE 1, sin lógica de sincronización todavía)");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
