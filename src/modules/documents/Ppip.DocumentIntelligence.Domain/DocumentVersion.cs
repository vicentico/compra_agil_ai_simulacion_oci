using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>
/// Una descarga concreta del documento — el binario (hash/storageRef) es
/// inmutable, nunca se modifica ni se borra (docs/03-domain/02, docs/08-data/02
/// source of truth). Lo que sí evoluciona sobre ese binario inmutable es el
/// resultado del pipeline de inteligencia documental (FASE 8: clasificación,
/// páginas extraídas, etapa de procesamiento) — mutación controlada
/// exclusivamente por <see cref="Document"/>, dueño del agregado.
/// </summary>
public sealed class DocumentVersion : Entity<Guid>
{
    public Sha256Hash Sha256Hash { get; }
    public StorageRef StorageRef { get; }
    public long SizeBytes { get; }
    public DateTimeOffset DownloadedAt { get; }

    public DocumentProcessingStage ProcessingStage { get; private set; }
    public DocumentClass? Classification { get; private set; }
    public string? ProcessingFailureReason { get; private set; }

    private readonly List<DocumentPage> _pages;
    public IReadOnlyList<DocumentPage> Pages => _pages;

    private DocumentVersion(
        Guid id,
        Sha256Hash sha256Hash,
        StorageRef storageRef,
        long sizeBytes,
        DateTimeOffset downloadedAt,
        DocumentProcessingStage processingStage,
        DocumentClass? classification,
        string? processingFailureReason,
        IEnumerable<DocumentPage> pages)
        : base(id)
    {
        Sha256Hash = sha256Hash;
        StorageRef = storageRef;
        SizeBytes = sizeBytes;
        DownloadedAt = downloadedAt;
        ProcessingStage = processingStage;
        Classification = classification;
        ProcessingFailureReason = processingFailureReason;
        _pages = [.. pages];
    }

    public static DocumentVersion Create(Sha256Hash sha256Hash, StorageRef storageRef, long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), sizeBytes, "El tamaño del binario debe ser mayor a cero.");
        }

        return new DocumentVersion(Guid.CreateVersion7(), sha256Hash, storageRef, sizeBytes, DateTimeOffset.UtcNow, DocumentProcessingStage.Pending, null, null, []);
    }

    /// <summary>Usado por los repositorios para reconstruir el agregado desde almacenamiento — no es un caso de uso de negocio.</summary>
    public static DocumentVersion Rehydrate(
        Guid id,
        Sha256Hash sha256Hash,
        StorageRef storageRef,
        long sizeBytes,
        DateTimeOffset downloadedAt,
        DocumentProcessingStage processingStage,
        DocumentClass? classification,
        string? processingFailureReason,
        IEnumerable<DocumentPage> pages) =>
        new(id, sha256Hash, storageRef, sizeBytes, downloadedAt, processingStage, classification, processingFailureReason, pages);

    /// <summary>UC-003 pasos 4-8 (clasificación + extracción + OCR ya unificados en las páginas finales) — solo <see cref="Document"/> lo invoca, para poder levantar el evento de integración.</summary>
    internal void CompleteExtraction(DocumentClass classification, IEnumerable<DocumentPage> pages)
    {
        if (ProcessingStage == DocumentProcessingStage.Chunked)
        {
            throw new InvalidOperationException("No se puede volver a extraer un documento ya con chunking completo — reprocesar debe partir de una versión nueva.");
        }

        Classification = classification;
        _pages.Clear();
        _pages.AddRange(pages);
        ProcessingStage = DocumentProcessingStage.Extracted;
        ProcessingFailureReason = null;
    }

    /// <summary>UC-003 paso 9.</summary>
    internal void MarkChunked()
    {
        if (ProcessingStage != DocumentProcessingStage.Extracted)
        {
            throw new InvalidOperationException($"No se puede completar el chunking desde {ProcessingStage}.");
        }

        ProcessingStage = DocumentProcessingStage.Chunked;
    }

    /// <summary>F3/F6 (docs/14-reliability): falla de clasificación/extracción/OCR/chunking — reintentable manualmente (vuelve a Pending vía CompleteExtraction).</summary>
    internal void MarkProcessingFailed(string reason)
    {
        ProcessingStage = DocumentProcessingStage.ProcessingFailed;
        ProcessingFailureReason = reason;
    }
}
