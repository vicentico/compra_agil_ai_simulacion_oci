namespace Ppip.DocumentIntelligence.Domain.Ports;

/// <summary>Puerto de <see cref="DocumentChunk"/> (colección `document_chunks`, docs/08-data/01). Adaptador Mongo real en FASE 8.</summary>
public interface IDocumentChunkRepository
{
    Task SaveManyAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentChunk>> FindByVersionAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>FASE 9: resuelve texto de chunks tras una búsqueda vectorial — el payload de Qdrant no incluye texto (docs/08-data).</summary>
    Task<IReadOnlyList<DocumentChunk>> FindByIdsAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken = default);

    /// <summary>FASE 9: vincula el chunk con su embedding (idempotente, ver <see cref="DocumentChunk.MarkEmbedded"/>).</summary>
    Task MarkEmbeddedAsync(Guid chunkId, Guid embeddingId, CancellationToken cancellationToken = default);
}
