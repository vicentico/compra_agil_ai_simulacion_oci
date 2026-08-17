using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>Una descarga concreta del documento — inmutable, nunca se modifica ni se borra (docs/03-domain/02, docs/08-data/02 source of truth).</summary>
public sealed class DocumentVersion : Entity<Guid>
{
    public Sha256Hash Sha256Hash { get; }
    public StorageRef StorageRef { get; }
    public long SizeBytes { get; }
    public DateTimeOffset DownloadedAt { get; }

    private DocumentVersion(Guid id, Sha256Hash sha256Hash, StorageRef storageRef, long sizeBytes, DateTimeOffset downloadedAt) : base(id)
    {
        Sha256Hash = sha256Hash;
        StorageRef = storageRef;
        SizeBytes = sizeBytes;
        DownloadedAt = downloadedAt;
    }

    public static DocumentVersion Create(Sha256Hash sha256Hash, StorageRef storageRef, long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), sizeBytes, "El tamaño del binario debe ser mayor a cero.");
        }

        return new DocumentVersion(Guid.CreateVersion7(), sha256Hash, storageRef, sizeBytes, DateTimeOffset.UtcNow);
    }

    /// <summary>Usado por los repositorios para reconstruir el agregado desde almacenamiento — no es un caso de uso de negocio.</summary>
    public static DocumentVersion Rehydrate(Guid id, Sha256Hash sha256Hash, StorageRef storageRef, long sizeBytes, DateTimeOffset downloadedAt) =>
        new(id, sha256Hash, storageRef, sizeBytes, downloadedAt);
}
