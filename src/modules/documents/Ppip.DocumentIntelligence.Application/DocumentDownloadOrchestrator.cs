using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Exceptions;
using Ppip.DocumentIntelligence.Domain.Policies;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Application;

/// <summary>
/// Orquesta UC-003 pasos 1-3: valida (allowlist/tipo/tamaño, FR-010) →
/// descarga → verifica magic bytes → escanea → hashea → guarda en object
/// storage → registra versión → publica evento. Idempotente: un documento ya
/// en etapa terminal (Downloaded/RejectedByPolicy) no se reprocesa; un
/// binario con el mismo hash que la versión actual no genera versión nueva
/// ni evento (NFR-002).
/// </summary>
public sealed class DocumentDownloadOrchestrator(
    IDocumentRepository documents,
    IObjectStorage storage,
    IAttachmentDownloader downloader,
    IMalwareScanner malwareScanner,
    DocumentEventPublisher publisher,
    IOptions<DocumentDownloadOptions> options,
    ILogger<DocumentDownloadOrchestrator> logger)
{
    public async Task<Document> ProcessAsync(string compraAgilId, string sourceUrl, string declaredName, string correlationId, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var existing = await documents.FindByCompraAndUrlAsync(compraAgilId, sourceUrl, cancellationToken);

        if (existing is not null && existing.Stage is DocumentStage.Downloaded or DocumentStage.RejectedByPolicy)
        {
            logger.LogInformation("Documento {DocumentId} ya está en etapa terminal {Stage}, no se reprocesa.", existing.Id, existing.Stage);
            return existing;
        }

        var document = existing ?? Document.Detect(DocumentId.New(), compraAgilId, sourceUrl, declaredName, correlationId);
        if (existing is null)
        {
            await documents.SaveAsync(document, cancellationToken);
            await publisher.PublishDetectedAsync(document, correlationId, opts.Producer, cancellationToken);
        }

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var url) || !UrlAllowlistPolicy.IsAllowed(url, opts.AllowedUrlPatterns))
        {
            return await RejectAsync(document, "URL fuera de allowlist o no HTTPS (FR-010).", cancellationToken);
        }

        DownloadedAttachment attachment;
        try
        {
            attachment = await downloader.DownloadAsync(url, opts.MaxSizeBytes, cancellationToken);
        }
        catch (AttachmentBlockedException ex)
        {
            return await RejectAsync(document, ex.Message, cancellationToken);
        }
        catch (AttachmentTooLargeException ex)
        {
            return await RejectAsync(document, ex.Message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // F4 (UC-003 A1): backoff/reintentos ya los agotó IAttachmentDownloader — esto es el fallo final del intento actual.
            logger.LogWarning(ex, "Descarga fallida para documento {DocumentId}.", document.Id);
            document.MarkDownloadFailed(ex.Message);
            await documents.SaveAsync(document, cancellationToken);
            return document;
        }

        if (attachment.ContentType is null || !opts.AllowedContentTypes.Contains(attachment.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return await RejectAsync(document, $"Content-Type no permitido: '{attachment.ContentType}'.", cancellationToken);
        }

        // Acoplado a PDF a propósito (ASM-02: adjuntos mayoritariamente PDF) — si AllowedContentTypes
        // se extiende a otros formatos, esta verificación necesita su propia firma de magic bytes.
        if (!PdfMagicBytes.Matches(attachment.Content))
        {
            return await RejectAsync(document, "El binario no coincide con la firma esperada (magic bytes).", cancellationToken);
        }

        var scan = await malwareScanner.ScanAsync(attachment.Content, cancellationToken);
        if (!scan.IsClean)
        {
            return await RejectAsync(document, $"Escaneo de malware: {scan.Detail}", cancellationToken);
        }

        var hash = Sha256Hash.From(Convert.ToHexStringLower(SHA256.HashData(attachment.Content)));
        var key = $"{document.CompraAgilId}/original/{hash.Value}-{document.DeclaredName}";
        var storageRef = await storage.SaveAsync(opts.Bucket, key, attachment.Content, attachment.ContentType, cancellationToken);
        var version = DocumentVersion.Create(hash, storageRef, attachment.Content.LongLength);

        var isNewVersion = document.CompleteDownload(version, correlationId);
        await documents.SaveAsync(document, cancellationToken);

        if (isNewVersion)
        {
            await publisher.PublishDownloadedAsync(document, correlationId, opts.Producer, cancellationToken);
        }

        return document;
    }

    private async Task<Document> RejectAsync(Document document, string reason, CancellationToken cancellationToken)
    {
        logger.LogWarning("Documento {DocumentId} rechazado por política: {Reason}", document.Id, reason);
        document.RejectByPolicy(reason);
        await documents.SaveAsync(document, cancellationToken);
        return document;
    }
}
