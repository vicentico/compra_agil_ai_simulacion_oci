using Ppip.BuildingBlocks.Domain;

namespace Ppip.Knowledge.Domain;

/// <summary>
/// Referencia a un vector indexado (docs/03-domain/02): "El vector vive en
/// Qdrant; el dominio guarda la referencia" — nunca se persiste el vector
/// en sí acá, solo el <see cref="VectorRef"/> (Qdrant pointId) para poder
/// reconstruir/reconciliar por hash (docs/08-data source of truth: Qdrant
/// es índice derivado, nunca fuente primaria).
/// </summary>
public sealed class Embedding : Entity<Guid>
{
    public Guid ChunkId { get; }
    public string ModelVersion { get; }
    public int Dimension { get; }
    public string VectorRef { get; }
    public DateTimeOffset CreatedAt { get; }

    private Embedding(Guid id, Guid chunkId, string modelVersion, int dimension, string vectorRef, DateTimeOffset createdAt) : base(id)
    {
        ChunkId = chunkId;
        ModelVersion = modelVersion;
        Dimension = dimension;
        VectorRef = vectorRef;
        CreatedAt = createdAt;
    }

    public static Embedding Create(Guid chunkId, string modelVersion, int dimension, string vectorRef)
    {
        if (string.IsNullOrWhiteSpace(modelVersion))
        {
            throw new ArgumentException("La versión del modelo de embeddings es obligatoria.", nameof(modelVersion));
        }

        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "La dimensión debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(vectorRef))
        {
            throw new ArgumentException("La referencia al vector (Qdrant pointId) es obligatoria.", nameof(vectorRef));
        }

        return new Embedding(Guid.CreateVersion7(), chunkId, modelVersion.Trim(), dimension, vectorRef.Trim(), DateTimeOffset.UtcNow);
    }

    /// <summary>Usado por los repositorios para reconstruir desde almacenamiento.</summary>
    public static Embedding Rehydrate(Guid id, Guid chunkId, string modelVersion, int dimension, string vectorRef, DateTimeOffset createdAt) =>
        new(id, chunkId, modelVersion, dimension, vectorRef, createdAt);
}
