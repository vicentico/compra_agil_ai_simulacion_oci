using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;
using Ppip.Knowledge.Domain;
using Ppip.Knowledge.Domain.Ports;

namespace Ppip.Knowledge.Application;

/// <summary>
/// Orquesta docs/09 etapas 10-11 (Embedding/Indexing, FASE 9) sobre los chunks
/// de la versión actual de un <see cref="Document"/> ya chunkeado (FASE 8):
/// embebe cada chunk sin <see cref="DocumentChunk.EmbeddingId"/> → upsert en
/// Qdrant → persiste la referencia → publica EmbeddingCreated.v1. Idempotente
/// por chunk (un chunk ya embebido no se reprocesa), igual que
/// <c>DocumentProcessingOrchestrator</c> lo es por documento (FASE 8).
/// </summary>
public sealed class EmbeddingIndexer(
    IDocumentRepository documents,
    IDocumentChunkRepository chunks,
    IEmbeddingRepository embeddingRepository,
    IEmbeddingProvider embeddingProvider,
    IVectorIndex vectorIndex,
    KnowledgeEventPublisher publisher,
    IOptions<EmbeddingIndexingOptions> options,
    ILogger<EmbeddingIndexer> logger)
{
    public async Task<int> IndexAsync(DocumentId documentId, string correlationId, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var document = await documents.FindAsync(documentId, cancellationToken)
            ?? throw new InvalidOperationException($"El documento {documentId} no existe.");

        var version = document.CurrentVersion
            ?? throw new InvalidOperationException($"El documento {documentId} no tiene ninguna versión descargada todavía.");

        if (version.ProcessingStage != DocumentProcessingStage.Chunked)
        {
            throw new InvalidOperationException($"El documento {documentId} versión {version.Id} todavía no completó chunking (docs/09 etapa 9).");
        }

        var versionChunks = await chunks.FindByVersionAsync(version.Id, cancellationToken);
        var pending = versionChunks.Where(c => c.EmbeddingId is null).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Documento {DocumentId} versión {VersionId} ya tiene todos sus chunks embebidos, no se reprocesa.", documentId, version.Id);
            return 0;
        }

        var modelVersion = string.Empty;
        foreach (var chunk in pending)
        {
            var vector = await embeddingProvider.EmbedAsync(chunk.Text, cancellationToken);
            modelVersion = vector.ModelVersion;
            var embedding = Embedding.Create(chunk.Id, vector.ModelVersion, vector.Dimension, chunk.Id.ToString());

            var payload = new VectorPayload(
                CompraAgilId: chunk.CompraAgilId,
                DocumentId: chunk.DocumentId.Value,
                VersionId: chunk.VersionId,
                Page: chunk.Page,
                Section: chunk.Section,
                ChunkType: chunk.ChunkType.ToString(),
                Source: opts.Source,
                Hash: chunk.Hash,
                IsDemoData: opts.IsDemoData);

            await vectorIndex.UpsertAsync(new VectorPoint(embedding.VectorRef, vector.Values, payload), cancellationToken);
            await embeddingRepository.SaveAsync(embedding, cancellationToken);
            await chunks.MarkEmbeddedAsync(chunk.Id, embedding.Id, cancellationToken);
        }

        var isLastOfCompra = await IsLastOfCompraAsync(document.CompraAgilId, cancellationToken);
        await publisher.PublishEmbeddingCreatedAsync(
            documentId.ToString(), version.Id.ToString(), modelVersion, pending.Count, isLastOfCompra, correlationId, opts.Producer, cancellationToken);

        return pending.Count;
    }

    /// <summary>
    /// EmbeddingCreated.v1.isLastOfCompra (docs/07-events): true cuando todos
    /// los documentos de la compra ya completaron chunking Y tienen el 100%
    /// de sus chunks embebidos — señal para que AI Worker dispare el análisis
    /// completo (FASE 10, sin consumidor real todavía, ver docs/ROADMAP.md).
    /// </summary>
    private async Task<bool> IsLastOfCompraAsync(string compraAgilId, CancellationToken cancellationToken)
    {
        var siblings = await documents.FindByCompraAsync(compraAgilId, cancellationToken);
        foreach (var sibling in siblings)
        {
            var version = sibling.CurrentVersion;
            if (version is null || version.ProcessingStage != DocumentProcessingStage.Chunked)
            {
                return false;
            }

            var siblingChunks = await chunks.FindByVersionAsync(version.Id, cancellationToken);
            if (siblingChunks.Count == 0 || siblingChunks.Any(c => c.EmbeddingId is null))
            {
                return false;
            }
        }

        return true;
    }
}
