using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ppip.DocumentIntelligence.Infrastructure.Persistence;

/// <summary>Modelo de persistencia de `documents` (docs/08-data/01) — separado del agregado de dominio (constructores privados por diseño).</summary>
internal sealed class DocumentRecord
{
    // MongoDB.Driver 3.x exige representación explícita para Guid — sin esto,
    // serializar/filtrar por un Guid lanza en runtime ("GuidRepresentation is
    // Unspecified"), hallazgo real de FASE 7 (ver también MongoOutboxStore de
    // Ppip.Procurement.Infrastructure, mismo fix aplicado ahí).
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public string CompraAgilId { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public string DeclaredName { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string? FailureReason { get; set; }
}

/// <summary>
/// Modelo de persistencia de `document_versions` (docs/08-data/01). El
/// binario (Sha256/Bucket/Key/SizeBytes/DownloadedAt) es inmutable, pero los
/// campos de procesamiento (FASE 8) sí se actualizan sobre la misma versión
/// a medida que avanza el pipeline — por eso el repositorio hace upsert por
/// Id, no solo insert de versiones nuevas por hash.
/// </summary>
internal sealed class DocumentVersionRecord
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid DocumentId { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset DownloadedAt { get; set; }

    public string ProcessingStage { get; set; } = string.Empty;

    public string? Classification { get; set; }

    public string? ProcessingFailureReason { get; set; }
}

/// <summary>Modelo de persistencia de `document_pages` (docs/08-data/01) — colección propia, consultada por VersionId.</summary>
internal sealed class DocumentPageRecord
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid VersionId { get; set; }

    public int PageNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public string ExtractionMethod { get; set; } = string.Empty;

    public double TextDensity { get; set; }

    public double? OcrConfidence { get; set; }
}
