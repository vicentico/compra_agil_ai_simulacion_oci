using System.Text.Json.Serialization;

namespace Ppip.DocumentIntelligence.Application.Events;

/// <summary>Payload de <c>DocumentDetected.v1</c> (docs/07-events/00-event-conventions.md catálogo).</summary>
public sealed record DocumentDetectedPayload(
    [property: JsonPropertyName("documentId")] string DocumentId,
    [property: JsonPropertyName("compraAgilId")] string CompraAgilId,
    [property: JsonPropertyName("sourceUrl")] string SourceUrl,
    [property: JsonPropertyName("declaredName")] string DeclaredName);

/// <summary>Payload de <c>DocumentDownloaded.v1</c>.</summary>
public sealed record DocumentDownloadedPayload(
    [property: JsonPropertyName("documentId")] string DocumentId,
    [property: JsonPropertyName("compraAgilId")] string CompraAgilId,
    [property: JsonPropertyName("versionId")] string VersionId,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("storageRef")] StorageRefPayload StorageRef,
    [property: JsonPropertyName("sizeBytes")] long SizeBytes);

public sealed record StorageRefPayload(
    [property: JsonPropertyName("bucket")] string Bucket,
    [property: JsonPropertyName("key")] string Key);

/// <summary>Payload de <c>DocumentExtracted.v1</c> — clasificación + extracción de texto (+ OCR si aplicó ya unificado en las páginas).</summary>
public sealed record DocumentExtractedPayload(
    [property: JsonPropertyName("documentId")] string DocumentId,
    [property: JsonPropertyName("versionId")] string VersionId,
    [property: JsonPropertyName("pages")] int Pages,
    [property: JsonPropertyName("classification")] string Classification,
    [property: JsonPropertyName("textDensity")] double TextDensity);

/// <summary>Payload de <c>OcrCompleted.v1</c> — solo se publica si al menos una página pasó por OCR (FR-014).</summary>
public sealed record OcrCompletedPayload(
    [property: JsonPropertyName("documentId")] string DocumentId,
    [property: JsonPropertyName("versionId")] string VersionId,
    [property: JsonPropertyName("pagesOcr")] IReadOnlyList<int> PagesOcr,
    [property: JsonPropertyName("avgConfidence")] double AvgConfidence);

/// <summary>Payload de <c>DocumentChunked.v1</c>.</summary>
public sealed record DocumentChunkedPayload(
    [property: JsonPropertyName("documentId")] string DocumentId,
    [property: JsonPropertyName("versionId")] string VersionId,
    [property: JsonPropertyName("chunkCount")] int ChunkCount,
    [property: JsonPropertyName("chunkIds")] IReadOnlyList<string> ChunkIds);
