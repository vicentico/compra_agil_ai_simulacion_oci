namespace Ppip.DocumentWorker;

/// <summary>
/// Placeholder de FASE 1. La lógica real del pipeline documental de 11
/// etapas (descarga, storage, clasificación, extracción, OCR, chunking,
/// embedding, indexación) se implementa en FASE 7-9 — ver
/// docs/09-document-intelligence/01-document-pipeline.md.
/// </summary>
public sealed class HeartbeatWorker(ILogger<HeartbeatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("document-worker heartbeat (esqueleto FASE 1, sin pipeline documental todavía)");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
