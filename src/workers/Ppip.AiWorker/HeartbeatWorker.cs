namespace Ppip.AiWorker;

/// <summary>
/// Placeholder de FASE 1. La lógica real (análisis IA, extracción de
/// requisitos, generación de secciones de propuesta vía ILlmProvider) se
/// implementa en FASE 10-13 — ver docs/11-ai/01-ai-governance.md y ADR-007.
/// </summary>
public sealed class HeartbeatWorker(ILogger<HeartbeatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ai-worker heartbeat (esqueleto FASE 1, sin ILlmProvider todavía)");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
