namespace Ppip.DocumentIntelligence.Domain.Ports;

/// <summary>Puerto de object storage (ADR-004: MinIO, bucket `chilecompra`). Adaptador real en FASE 7.</summary>
public interface IObjectStorage
{
    Task<StorageRef> SaveAsync(string bucket, string key, byte[] content, string? contentType, CancellationToken cancellationToken = default);
}
