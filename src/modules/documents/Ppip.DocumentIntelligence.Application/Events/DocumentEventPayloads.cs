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
