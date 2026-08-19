namespace Ppip.DocumentIntelligence.Domain.Ports;

/// <summary>Puerto de <see cref="DocumentChunk"/> (colección `document_chunks`, docs/08-data/01). Adaptador Mongo real en FASE 8.</summary>
public interface IDocumentChunkRepository
{
    Task SaveManyAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentChunk>> FindByVersionAsync(Guid versionId, CancellationToken cancellationToken = default);
}
