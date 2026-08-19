using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>
/// Agregado raíz de Document Intelligence (docs/03-domain/02-domain-model.md,
/// UC-003 pasos 1-3). Cubre solo descarga+storage (FASE 7); clasificación,
/// extracción, OCR y chunking llegan en FASE 8-9 con sus propios estados.
/// </summary>
public sealed class Document : AggregateRoot<DocumentId>
{
    public string CompraAgilId { get; }
    public string SourceUrl { get; }
    public string DeclaredName { get; }
    public DocumentStage Stage { get; private set; }
    public string? FailureReason { get; private set; }

    private readonly List<DocumentVersion> _versions;
    public IReadOnlyList<DocumentVersion> Versions => _versions;
    public DocumentVersion? CurrentVersion => _versions.Count > 0 ? _versions[^1] : null;

    private Document(
        DocumentId id,
        string compraAgilId,
        string sourceUrl,
        string declaredName,
        DocumentStage stage,
        string? failureReason,
        IEnumerable<DocumentVersion> versions)
        : base(id)
    {
        CompraAgilId = compraAgilId;
        SourceUrl = sourceUrl;
        DeclaredName = declaredName;
        Stage = stage;
        FailureReason = failureReason;
        _versions = [.. versions];
    }

    /// <summary>Registra un documento adjunto a procesar (UC-003 paso 1) — levanta <see cref="DocumentDetected"/>.</summary>
    public static Document Detect(DocumentId id, string compraAgilId, string sourceUrl, string declaredName, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(compraAgilId))
        {
            throw new ArgumentException("El id de la Compra Ágil es obligatorio.", nameof(compraAgilId));
        }

        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            throw new ArgumentException("La URL de origen es obligatoria.", nameof(sourceUrl));
        }

        if (string.IsNullOrWhiteSpace(declaredName))
        {
            throw new ArgumentException("El nombre declarado del archivo es obligatorio.", nameof(declaredName));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("El correlationId es obligatorio.", nameof(correlationId));
        }

        var document = new Document(id, compraAgilId.Trim(), sourceUrl.Trim(), declaredName.Trim(), DocumentStage.Detected, null, []);
        document.Raise(new DocumentDetected(Guid.CreateVersion7(), DateTimeOffset.UtcNow, id.ToString(), document.CompraAgilId, document.SourceUrl, correlationId));
        return document;
    }

    /// <summary>Reconstruye el agregado tal como quedó persistido — no levanta eventos.</summary>
    public static Document Rehydrate(
        DocumentId id,
        string compraAgilId,
        string sourceUrl,
        string declaredName,
        DocumentStage stage,
        string? failureReason,
        IEnumerable<DocumentVersion> versions) =>
        new(id, compraAgilId, sourceUrl, declaredName, stage, failureReason, versions);

    /// <summary>URL fuera de allowlist, tipo no permitido o tamaño excedido (FR-010) — se rechaza antes de intentar la descarga.</summary>
    public void RejectByPolicy(string reason)
    {
        if (Stage != DocumentStage.Detected)
        {
            throw new InvalidOperationException($"Solo se puede rechazar por política desde {DocumentStage.Detected} (etapa actual: {Stage}).");
        }

        Stage = DocumentStage.RejectedByPolicy;
        FailureReason = RequireReason(reason);
    }

    /// <summary>UC-003 A1: reintentos agotados. Reintentable manualmente más tarde (vuelve a intentar desde aquí).</summary>
    public void MarkDownloadFailed(string reason)
    {
        if (Stage is not (DocumentStage.Detected or DocumentStage.DownloadFailed))
        {
            throw new InvalidOperationException($"No se puede marcar la descarga como fallida desde {Stage}.");
        }

        Stage = DocumentStage.DownloadFailed;
        FailureReason = RequireReason(reason);
    }

    /// <summary>
    /// UC-003 pasos 2-4. Idempotente por hash (NFR-002, docs/14-reliability:
    /// "documentId + sha256"): si el binario descargado es igual al de la
    /// versión actual, es un no-op explícito — no agrega versión ni levanta
    /// evento. Devuelve <c>true</c> si se registró una versión nueva.
    /// </summary>
    public bool CompleteDownload(DocumentVersion version, string correlationId)
    {
        if (Stage is not (DocumentStage.Detected or DocumentStage.DownloadFailed or DocumentStage.Downloaded))
        {
            throw new InvalidOperationException($"No se puede completar una descarga desde {Stage}.");
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("El correlationId es obligatorio.", nameof(correlationId));
        }

        // CurrentVersion solo es no-nulo si Stage ya es Downloaded (es la
        // única forma de llegar a tener una versión) — MarkDownloadFailed no
        // se puede invocar desde Downloaded, así que esta rama de no-op solo
        // se alcanza reprocesando un documento ya descargado.
        if (CurrentVersion is { } current && current.Sha256Hash == version.Sha256Hash)
        {
            return false;
        }

        _versions.Add(version);
        Stage = DocumentStage.Downloaded;
        FailureReason = null;
        Raise(new DocumentDownloaded(Guid.CreateVersion7(), DateTimeOffset.UtcNow, Id.ToString(), CompraAgilId, version.Sha256Hash.Value, version.SizeBytes, correlationId));
        return true;
    }

    /// <summary>UC-003 pasos 4-8 (FASE 8): clasificación + extracción + OCR ya unificados en las páginas finales de <see cref="CurrentVersion"/> — levanta <see cref="DocumentExtracted"/>.</summary>
    public void CompleteExtraction(DocumentClass classification, IEnumerable<DocumentPage> pages, string correlationId)
    {
        var version = RequireCurrentVersion();
        var pageList = pages.ToList();
        version.CompleteExtraction(classification, pageList);

        var avgDensity = pageList.Count > 0 ? pageList.Average(p => p.TextDensity) : 0d;
        Raise(new DocumentExtracted(Guid.CreateVersion7(), DateTimeOffset.UtcNow, Id.ToString(), version.Id.ToString(), pageList.Count, classification.ToString(), avgDensity, correlationId));
    }

    /// <summary>Levanta <see cref="OcrCompleted"/> solo si al menos una página de <see cref="CurrentVersion"/> pasó por OCR (FR-014) — no-op silencioso para documentos puramente textuales.</summary>
    public void ReportOcrCompleted(string correlationId)
    {
        var version = RequireCurrentVersion();
        var ocrPages = version.Pages.Where(p => p.ExtractionMethod == ExtractionMethod.Ocr).ToList();
        if (ocrPages.Count == 0)
        {
            return;
        }

        var avgConfidence = ocrPages.Average(p => p.OcrConfidence ?? 0d);
        Raise(new OcrCompleted(Guid.CreateVersion7(), DateTimeOffset.UtcNow, Id.ToString(), version.Id.ToString(), [.. ocrPages.Select(p => p.PageNumber)], avgConfidence, correlationId));
    }

    /// <summary>UC-003 paso 9 — levanta <see cref="DocumentChunked"/>.</summary>
    public void CompleteChunking(IReadOnlyList<DocumentChunk> chunks, string correlationId)
    {
        var version = RequireCurrentVersion();
        version.MarkChunked();
        Raise(new DocumentChunked(Guid.CreateVersion7(), DateTimeOffset.UtcNow, Id.ToString(), version.Id.ToString(), chunks.Count, [.. chunks.Select(c => c.Id.ToString())], correlationId));
    }

    /// <summary>F3/F6 (docs/14-reliability): clasificación/extracción/OCR/chunking falló — reintentable manualmente (vuelve a intentar desde <see cref="CompleteExtraction"/>).</summary>
    public void MarkProcessingFailed(string reason) => RequireCurrentVersion().MarkProcessingFailed(RequireReason(reason));

    private DocumentVersion RequireCurrentVersion() =>
        CurrentVersion ?? throw new InvalidOperationException("El documento no tiene ninguna versión descargada todavía.");

    private static string RequireReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("El motivo es obligatorio.", nameof(reason));
        }

        return reason.Trim();
    }
}
