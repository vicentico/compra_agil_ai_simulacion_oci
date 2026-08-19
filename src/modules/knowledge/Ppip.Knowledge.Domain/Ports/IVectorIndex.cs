namespace Ppip.Knowledge.Domain.Ports;

/// <summary>
/// ADR-005: puerto hacia el índice vectorial (Qdrant). El payload jamás
/// incluye el texto del chunk (docs/08-data) — Knowledge.Application
/// resuelve el texto vía IDocumentChunkRepository (DocumentIntelligence.Domain)
/// después de la búsqueda.
/// </summary>
public interface IVectorIndex
{
    Task UpsertAsync(VectorPoint point, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtro por compraAgilId es obligatorio y jamás controlado por el LLM/cliente
    /// (ADR-008: aislamiento por proceso de compra).
    /// </summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] queryVector, string compraAgilId, int topK, CancellationToken cancellationToken = default);
}

public sealed record VectorPoint(string PointId, float[] Vector, VectorPayload Payload);

public sealed record VectorPayload(
    string CompraAgilId,
    Guid DocumentId,
    Guid VersionId,
    int Page,
    string? Section,
    string ChunkType,
    string Source,
    string Hash,
    bool IsDemoData);

public sealed record VectorSearchResult(string PointId, float Score, VectorPayload Payload);
