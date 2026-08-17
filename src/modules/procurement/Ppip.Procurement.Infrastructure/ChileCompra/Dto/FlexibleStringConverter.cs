using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ppip.Procurement.Infrastructure.ChileCompra.Dto;

/// <summary>
/// Hallazgo del spike de FASE 5: campos documentados como <c>string</c>
/// (p.ej. <c>documentos[].id</c>, documentado como UUID string) llegan en la
/// práctica como número JSON en algunos registros — igual que
/// <c>codigo_producto</c>, que la propia guía ya documenta como <c>int|string</c>
/// mixto. Acepta ambos y normaliza a string.
/// </summary>
public sealed class FlexibleStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var integer)
                ? integer.ToString(CultureInfo.InvariantCulture)
                : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"No se puede convertir el token {reader.TokenType} a string."),
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
