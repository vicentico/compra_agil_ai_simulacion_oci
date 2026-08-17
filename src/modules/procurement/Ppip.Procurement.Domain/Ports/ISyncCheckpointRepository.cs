namespace Ppip.Procurement.Domain.Ports;

/// <summary>Puerto de <see cref="SyncCheckpoint"/> (FR-005, NFR-013). Adaptador Mongo real en FASE 6.</summary>
public interface ISyncCheckpointRepository
{
    Task<SyncCheckpoint?> FindAsync(string source, CancellationToken cancellationToken = default);

    Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken cancellationToken = default);
}
