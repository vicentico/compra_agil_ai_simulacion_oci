using System.Text.Json.Serialization;

namespace Ppip.Procurement.Infrastructure.ChileCompra.Dto;

// Basado en la Guía de Uso API Compra Ágil v2 (v3.0, mayo 2026) §6.1, mas
// hallazgos del spike real de FASE 5 (docs/ROADMAP.md nota de cierre):
// - Fechas como string sin parsear: fecha_ultimo_cambio llega en ISO-8601
//   con milisegundos ("2026-08-16T23:05:02.410Z") pero fecha_publicacion y
//   fecha_cierre llegan en formato corto sin zona horaria ("2026-08-16 23:02")
//   — parsear ambos formatos ciegamente con DateTimeOffset rompería. Se
//   normaliza en la capa de aplicación (FASE 6) con ChileCompraDateParser.
// - fecha_cierre_primer_llamado / fecha_cierre_segundo_llamado NO están
//   documentados en el listado (la guía los ubica solo en el detalle, bajo
//   convocatoria) pero SÍ aparecen aquí en la respuesta real, dentro de
//   "fechas" y en formato ISO-8601 completo.

public sealed class CompraAgilListItemDto
{
    [JsonPropertyName("codigo")]
    public string Codigo { get; init; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("estado")]
    public EstadoDto Estado { get; init; } = new();

    [JsonPropertyName("convocatoria")]
    public ConvocatoriaListDto Convocatoria { get; init; } = new();

    [JsonPropertyName("documentos")]
    public IReadOnlyList<DocumentoRefDto> Documentos { get; init; } = [];

    [JsonPropertyName("fechas")]
    public FechasListDto Fechas { get; init; } = new();

    [JsonPropertyName("montos")]
    public MontosDto Montos { get; init; } = new();

    [JsonPropertyName("institucion")]
    public InstitucionDto Institucion { get; init; } = new();

    [JsonPropertyName("resumen")]
    public ResumenListDto Resumen { get; init; } = new();

    [JsonPropertyName("motivos")]
    public MotivosListDto Motivos { get; init; } = new();

    [JsonPropertyName("links")]
    public LinksDto Links { get; init; } = new();
}

public sealed class EstadoDto
{
    [JsonPropertyName("id_estado")]
    public int IdEstado { get; init; }

    /// <summary>publicada | cerrada | desierta | cancelada | proveedor_seleccionado (oc_emitida definido pero no usado en la práctica).</summary>
    [JsonPropertyName("codigo")]
    public string Codigo { get; init; } = string.Empty;

    [JsonPropertyName("glosa")]
    public string Glosa { get; init; } = string.Empty;
}

public sealed class ConvocatoriaListDto
{
    /// <summary>1 = primer llamado, 2 = segundo llamado.</summary>
    [JsonPropertyName("estado_convocatoria")]
    public int EstadoConvocatoria { get; init; }

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; init; } = string.Empty;
}

public sealed class DocumentoRefDto
{
    // Documentado como string (UUID) pero observado como número en algunos
    // registros reales — ver FlexibleStringConverter.
    [JsonPropertyName("id")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class FechasListDto
{
    [JsonPropertyName("fecha_publicacion")]
    public string? FechaPublicacion { get; init; }

    [JsonPropertyName("fecha_cierre")]
    public string? FechaCierre { get; init; }

    [JsonPropertyName("fecha_ultimo_cambio")]
    public string? FechaUltimoCambio { get; init; }

    [JsonPropertyName("fecha_cancelacion")]
    public string? FechaCancelacion { get; init; }

    /// <summary>No documentado en §6.1 (la guía solo lo ubica en el detalle) — observado en la respuesta real.</summary>
    [JsonPropertyName("fecha_cierre_primer_llamado")]
    public string? FechaCierrePrimerLlamado { get; init; }

    /// <summary>No documentado en §6.1 — observado en la respuesta real.</summary>
    [JsonPropertyName("fecha_cierre_segundo_llamado")]
    public string? FechaCierreSegundoLlamado { get; init; }
}

public sealed class MontosDto
{
    [JsonPropertyName("moneda")]
    public string Moneda { get; init; } = string.Empty;

    [JsonPropertyName("monto_disponible")]
    public decimal? MontoDisponible { get; init; }

    [JsonPropertyName("monto_disponible_clp")]
    public decimal? MontoDisponibleClp { get; init; }
}

public sealed class InstitucionDto
{
    [JsonPropertyName("organismo_comprador")]
    public string OrganismoComprador { get; init; } = string.Empty;

    [JsonPropertyName("rut")]
    public string Rut { get; init; } = string.Empty;

    [JsonPropertyName("unidad_compra")]
    public string UnidadCompra { get; init; } = string.Empty;

    [JsonPropertyName("region")]
    public int? Region { get; init; }

    [JsonPropertyName("nombre_region")]
    public string? NombreRegion { get; init; }
}

public sealed class ResumenListDto
{
    [JsonPropertyName("total_ofertas_recibidas")]
    public int TotalOfertasRecibidas { get; init; }
}

public sealed class MotivosListDto
{
    [JsonPropertyName("motivo_cancelacion")]
    public string? MotivoCancelacion { get; init; }

    [JsonPropertyName("motivo_desierta")]
    public string? MotivoDesierta { get; init; }

    [JsonPropertyName("motivo_seleccion")]
    public string? MotivoSeleccion { get; init; }
}

public sealed class LinksDto
{
    [JsonPropertyName("detalle")]
    public string Detalle { get; init; } = string.Empty;
}

public sealed class PaginacionDto
{
    [JsonPropertyName("total_paginas")]
    public int TotalPaginas { get; init; }

    [JsonPropertyName("numero_pagina")]
    public int NumeroPagina { get; init; }

    [JsonPropertyName("tamano_pagina")]
    public int TamanoPagina { get; init; }

    [JsonPropertyName("total_resultados")]
    public int TotalResultados { get; init; }
}

public sealed class CompraAgilListPayloadDto
{
    [JsonPropertyName("items")]
    public IReadOnlyList<CompraAgilListItemDto> Items { get; init; } = [];

    [JsonPropertyName("paginacion")]
    public PaginacionDto Paginacion { get; init; } = new();
}
