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

/// <summary>Modelo de persistencia de `document_versions` (docs/08-data/01: unique index `{documentId, sha256}`) — append-only, nunca se actualiza ni borra.</summary>
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
}
