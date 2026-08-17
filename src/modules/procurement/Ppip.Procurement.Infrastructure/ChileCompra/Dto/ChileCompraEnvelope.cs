using System.Text.Json.Serialization;

namespace Ppip.Procurement.Infrastructure.ChileCompra.Dto;

/// <summary>
/// Envelope de toda respuesta de la API Compra Ágil v2 (Guía de Uso v3.0
/// §6/§7): éxito trae <c>payload</c> con <c>errors: null</c>; error trae
/// <c>payload: null</c> con al menos un elemento en <c>errors</c>.
/// </summary>
public sealed class ChileCompraEnvelope<TPayload>
{
    [JsonPropertyName("success")]
    public string Success { get; init; } = string.Empty;

    [JsonPropertyName("trace")]
    public string? Trace { get; init; }

    [JsonPropertyName("payload")]
    public TPayload? Payload { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<ChileCompraErrorDto>? Errors { get; init; }

    public bool IsSuccess => Success == "OK";
}
