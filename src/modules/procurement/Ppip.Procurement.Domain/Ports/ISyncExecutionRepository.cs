namespace Ppip.Procurement.Domain.Ports;

/// <summary>Puerto de <see cref="SyncExecution"/> (bitácora de ciclos de sync, NFR-013). Adaptador Mongo real en FASE 6.</summary>
public interface ISyncExecutionRepository
{
    Task SaveAsync(SyncExecution execution, CancellationToken cancellationToken = default);
}
