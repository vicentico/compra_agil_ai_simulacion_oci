using System.Text.Json.Serialization;

namespace Ppip.Procurement.Application.Events;

/// <summary>
/// Payload de <c>CompraAgilDetected.v1</c>
/// (docs/07-events/schemas/CompraAgilDetected.v1.schema.json) — los
/// <c>JsonPropertyName</c> son obligatorios, el schema exige camelCase.
/// </summary>
public sealed record CompraAgilDetectedPayload(
    [property: JsonPropertyName("compraAgilId")] string CompraAgilId,
    [property: JsonPropertyName("codigo")] string Codigo,
    [property: JsonPropertyName("nombre")] string Nombre,
    [property: JsonPropertyName("organismoCodigo")] string OrganismoCodigo,
    [property: JsonPropertyName("fechaCierre")] DateTimeOffset FechaCierre,
    [property: JsonPropertyName("montoDisponible")] MoneyPayload MontoDisponible,
    [property: JsonPropertyName("rawPayloadId")] string RawPayloadId,
    [property: JsonPropertyName("documentRefs")] IReadOnlyList<DocumentRefPayload> DocumentRefs);

/// <summary>Payload de <c>CompraAgilUpdated.v1</c> (docs/07-events/schemas/CompraAgilUpdated.v1.schema.json).</summary>
public sealed record CompraAgilUpdatedPayload(
    [property: JsonPropertyName("compraAgilId")] string CompraAgilId,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("changedFields")] IReadOnlyList<string> ChangedFields,
    [property: JsonPropertyName("rawPayloadId")] string RawPayloadId);

public sealed record MoneyPayload(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency);

/// <summary>
/// <c>declaredName</c> es opcional en el schema; <c>sourceUrl</c> es
/// obligatorio y debe ser una URI real — OQ-02 (endpoint de descarga de
/// adjuntos) sigue abierta (docs/01-discovery/09-open-questions.md), así que
/// FASE 6 todavía no puede construir uno honesto. Por eso
/// <see cref="Application.ProcurementEventPublisher"/> publica siempre
/// <c>documentRefs: []</c> aunque el item traiga documentos — se completa
/// cuando OQ-02 se cierre (spike de FASE 7).
/// </summary>
public sealed record DocumentRefPayload(
    [property: JsonPropertyName("documentId")] string DocumentId,
    [property: JsonPropertyName("sourceUrl")] string SourceUrl,
    [property: JsonPropertyName("declaredName")] string? DeclaredName);
