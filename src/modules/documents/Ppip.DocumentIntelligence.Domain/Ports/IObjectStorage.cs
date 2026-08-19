namespace Ppip.DocumentIntelligence.Domain.Ports;

/// <summary>Puerto de object storage (ADR-004: MinIO, bucket `chilecompra`). Adaptador real en FASE 7 (Save) + FASE 8 (Load, para que la etapa de clasificación/extracción lea el binario ya guardado).</summary>
public interface IObjectStorage
{
    Task<StorageRef> SaveAsync(string bucket, string key, byte[] content, string? contentType, CancellationToken cancellationToken = default);

    Task<byte[]> LoadAsync(StorageRef storageRef, CancellationToken cancellationToken = default);
}
