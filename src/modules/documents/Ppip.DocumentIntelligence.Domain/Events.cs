using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>Se levanta al registrar un documento adjunto a procesar (UC-003 paso 1). Payload mínimo — docs/07-events/00, regla 6.</summary>
public sealed record DocumentDetected(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string DocumentId,
    string CompraAgilId,
    string SourceUrl,
    string CorrelationId) : IDomainEvent;

/// <summary>Se levanta cuando una descarga produce una versión nueva (hash distinto de la versión anterior, o primera versión) — UC-003 pasos 2-4.</summary>
public sealed record DocumentDownloaded(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string DocumentId,
    string CompraAgilId,
    string Sha256Hash,
    long SizeBytes,
    string CorrelationId) : IDomainEvent;

/// <summary>Se levanta al completar clasificación + extracción de texto (+ OCR si aplicó) — UC-003 pasos 4-8.</summary>
public sealed record DocumentExtracted(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string DocumentId,
    string VersionId,
    int Pages,
    string Classification,
    double AvgTextDensity,
    string CorrelationId) : IDomainEvent;

/// <summary>Se levanta solo si al menos una página pasó por OCR (FR-014) — UC-003 paso 6.</summary>
public sealed record OcrCompleted(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string DocumentId,
    string VersionId,
    IReadOnlyList<int> PagesOcr,
    double AvgConfidence,
    string CorrelationId) : IDomainEvent;

/// <summary>Se levanta al completar el chunking semántico — UC-003 paso 9.</summary>
public sealed record DocumentChunked(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string DocumentId,
    string VersionId,
    int ChunkCount,
    IReadOnlyList<string> ChunkIds,
    string CorrelationId) : IDomainEvent;
