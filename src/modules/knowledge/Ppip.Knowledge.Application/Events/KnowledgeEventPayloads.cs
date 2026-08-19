using System.Text.Json.Serialization;

namespace Ppip.Knowledge.Application.Events;

/// <summary>Payload de <c>EmbeddingCreated.v1</c> (docs/07-events/00-event-conventions.md catálogo: consumido por AI Worker para disparar análisis, FASE 10).</summary>
public sealed record EmbeddingCreatedPayload(
    [property: JsonPropertyName("documentId")] string DocumentId,
    [property: JsonPropertyName("versionId")] string VersionId,
    [property: JsonPropertyName("modelVersion")] string ModelVersion,
    [property: JsonPropertyName("indexedCount")] int IndexedCount,
    [property: JsonPropertyName("isLastOfCompra")] bool IsLastOfCompra);
