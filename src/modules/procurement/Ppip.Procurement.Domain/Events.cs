using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>
/// Se levanta al detectar una Compra Ágil inexistente localmente (UC-001
/// paso 6). Payload mínimo — el consumidor consulta el estado actual por id
/// (docs/07-events/00, regla 6).
/// </summary>
public sealed record CompraAgilDetected(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string CompraAgilId,
    string RawPayloadHash,
    string CorrelationId) : IDomainEvent;

/// <summary>Se levanta cuando el hash del payload cambia respecto de la versión local (UC-001 paso 7).</summary>
public sealed record CompraAgilUpdated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string CompraAgilId,
    int Version,
    IReadOnlyList<string> ChangedFields,
    string RawPayloadHash,
    string CorrelationId) : IDomainEvent;
