using System.Text.Json.Serialization;

namespace Ppip.Procurement.Infrastructure.ChileCompra.Dto;

public sealed class ChileCompraErrorDto
{
    [JsonPropertyName("codigo")]
    public string Codigo { get; init; } = string.Empty;

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; init; } = string.Empty;

    [JsonPropertyName("detalle")]
    public string? Detalle { get; init; }
}
