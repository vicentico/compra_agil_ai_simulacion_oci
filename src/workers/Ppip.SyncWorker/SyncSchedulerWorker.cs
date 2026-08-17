using Microsoft.Extensions.Options;
using Ppip.Procurement.Application;

namespace Ppip.SyncWorker;

/// <summary>
/// Componente "Scheduler" de docs/04-architecture/03-component-diagram.md:
/// dispara <see cref="SyncOrchestrator"/> cada <c>Ppip:Sync:Interval</c>
/// (UC-001 flujo principal, paso 1). Un ciclo por intervalo — la exclusión
/// mutua real la garantiza el lock distribuido dentro del orquestador
/// (UC-001 A5), no este worker.
/// </summary>
public sealed class SyncSchedulerWorker(
    SyncOrchestrator orchestrator,
    IOptions<SyncOptions> options,
    ILogger<SyncSchedulerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.Interval;
        if (interval <= TimeSpan.Zero)
        {
            logger.LogInformation("SyncSchedulerWorker deshabilitado (Ppip:Sync:Interval <= 0) — solo disparo manual vía /internal/sync/trigger.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var correlationId = $"sync-scheduler-{Guid.CreateVersion7()}";
            try
            {
                var execution = await orchestrator.RunAsync(correlationId, stoppingToken);
                logger.LogInformation(
                    "Ciclo de sync {CorrelationId} → {Status} (created={Created}, updated={Updated}, unchanged={Unchanged}, errors={Errors}).",
                    correlationId, execution.Status, execution.Created, execution.Updated, execution.Unchanged, execution.Errors);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // El orquestador ya absorbe los errores esperables del ciclo
                // (F1-F3) marcando la ejecución Aborted; esto solo cubre un
                // fallo verdaderamente inesperado para que el scheduler no
                // muera y siga intentando en el próximo intervalo.
                logger.LogError(ex, "Ciclo de sync {CorrelationId} falló de forma inesperada.", correlationId);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
