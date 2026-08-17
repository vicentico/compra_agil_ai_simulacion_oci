using System.Globalization;

namespace Ppip.Procurement.Infrastructure.ChileCompra;

/// <summary>
/// La API Compra Ágil v2 no es consistente en el formato de sus campos de
/// fecha (hallazgo del spike de FASE 5, docs/ROADMAP.md): algunos llegan en
/// ISO-8601 completo con milisegundos y "Z" (p.ej. <c>fecha_ultimo_cambio</c>
/// del listado), otros en formato corto sin zona horaria asumiendo hora de
/// Chile (p.ej. <c>fecha_publicacion</c>, y <c>fecha_ultimo_cambio</c> del
/// *detalle* — el mismo campo, dos formatos distintos según el endpoint).
/// Por eso los DTO guardan fechas como <c>string</c> crudo — este parser las
/// normaliza cuando la capa de aplicación (FASE 6) las necesite como
/// <see cref="DateTimeOffset"/>.
/// </summary>
public static class ChileCompraDateParser
{
    private static readonly string[] KnownFormats =
    [
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-dd HH:mm",
    ];

    private static readonly TimeZoneInfo ChileTimeZone = ResolveChileTimeZone();

    /// <summary>
    /// Formatos con "Z" se interpretan como UTC. El formato corto no trae
    /// zona horaria — se asume hora de Chile (Continental), consistente con
    /// que la API es de un organismo público chileno.
    /// </summary>
    public static DateTimeOffset? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (var format in KnownFormats)
        {
            var hasZone = format.EndsWith('Z');
            var style = hasZone
                ? DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal
                : DateTimeStyles.AssumeLocal;

            if (!DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, style, out var parsed))
            {
                continue;
            }

            return hasZone
                ? new DateTimeOffset(parsed, TimeSpan.Zero)
                : new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), ChileTimeZone.GetUtcOffset(parsed));
        }

        return null;
    }

    private static TimeZoneInfo ResolveChileTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time"); // Windows sin datos IANA
        }
    }
}
