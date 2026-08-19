using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Application.Chunking;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Policies;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Application;

/// <summary>
/// Orquesta UC-003 pasos 4-9 (FASE 8) sobre la versión actual de un
/// <see cref="Document"/> ya descargado: carga el binario desde storage →
/// clasifica + extrae texto nativo → OCR en páginas de baja densidad (si
/// tienen imagen embebida extraíble) → chunking semántico → persiste chunks
/// → publica eventos. Idempotente a nivel de documento completo: una
/// versión ya en <see cref="DocumentProcessingStage.Chunked"/> no se
/// reprocesa; cualquier otro estado reintenta el pipeline completo desde el
/// binario ya guardado (determinístico salvo por el proveedor OCR real).
/// </summary>
public sealed class DocumentProcessingOrchestrator(
    IDocumentRepository documents,
    IObjectStorage storage,
    IPdfExtractor pdfExtractor,
    IOcrService ocrService,
    IDocumentChunkRepository chunkRepository,
    DocumentEventPublisher publisher,
    IOptions<DocumentProcessingOptions> options,
    ILogger<DocumentProcessingOrchestrator> logger)
{
    public async Task<Document> ProcessAsync(DocumentId documentId, string correlationId, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var document = await documents.FindAsync(documentId, cancellationToken)
            ?? throw new InvalidOperationException($"El documento {documentId} no existe.");

        var version = document.CurrentVersion
            ?? throw new InvalidOperationException($"El documento {documentId} no tiene ninguna versión descargada todavía.");

        if (version.ProcessingStage == DocumentProcessingStage.Chunked)
        {
            logger.LogInformation("Documento {DocumentId} versión {VersionId} ya tiene chunking completo, no se reprocesa.", documentId, version.Id);
            return document;
        }

        try
        {
            var binary = await storage.LoadAsync(version.StorageRef, cancellationToken);
            var extracted = pdfExtractor.Extract(binary);

            var pages = new List<DocumentPage>();
            foreach (var extractedPage in extracted.Pages)
            {
                var page = DocumentPage.FromNativeText(extractedPage.PageNumber, extractedPage.NativeText, extractedPage.TextDensity);

                if (ClassificationPolicy.RequiresOcr(extractedPage.TextDensity, opts.ScannedDensityThreshold) && extractedPage.EmbeddedImages.Count > 0)
                {
                    // Usa la imagen embebida más grande de la página — para
                    // un PDF genuinamente escaneado (el caso principal de
                    // FR-014), esa imagen ES la página completa. Ver
                    // Domain/Ports/IPdfExtractor.cs para la limitación
                    // exacta (no hay rasterización de contenido vectorial).
                    var largestImage = extractedPage.EmbeddedImages.OrderByDescending(i => i.Length).First();
                    var ocrResult = await ocrService.RecognizeAsync(largestImage, cancellationToken);
                    page.ApplyOcr(ocrResult.Text, ocrResult.Confidence);
                }

                pages.Add(page);
            }

            var classification = ClassificationPolicy.Classify(
                [.. extracted.Pages.Select(p => p.TextDensity)],
                extracted.Pages.Any(p => p.EmbeddedImages.Count > 0),
                extracted.Pages.Any(p => p.HasTableLikeLayout),
                opts.TextualDensityThreshold,
                opts.ScannedDensityThreshold);

            document.CompleteExtraction(classification, pages, correlationId);
            await documents.SaveAsync(document, cancellationToken);
            await publisher.PublishExtractedAsync(document, correlationId, opts.Producer, cancellationToken);
            await publisher.PublishOcrCompletedAsync(document, correlationId, opts.Producer, cancellationToken);

            var thresholds = new ChunkingThresholds(opts.TargetChunkTokens, opts.MaxChunkTokens, opts.ChunkOverlapTokens);
            var pendingChunks = ChunkingService.Chunk(pages, thresholds);
            var documentChunks = pendingChunks
                .Select(p => DocumentChunk.Create(document.Id, version.Id, document.CompraAgilId, p.Page, p.Section, p.SubSection, p.ChunkType, p.Text, p.TokenCount))
                .ToList();

            await chunkRepository.SaveManyAsync(documentChunks, cancellationToken);
            document.CompleteChunking(documentChunks, correlationId);
            await documents.SaveAsync(document, cancellationToken);
            await publisher.PublishChunkedAsync(document, documentChunks.Count, [.. documentChunks.Select(c => c.Id.ToString())], correlationId, opts.Producer, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Procesamiento (clasificación/extracción/OCR/chunking) fallido para documento {DocumentId}.", documentId);
            document.MarkProcessingFailed(ex.Message);
            await documents.SaveAsync(document, cancellationToken);
        }

        return document;
    }
}
