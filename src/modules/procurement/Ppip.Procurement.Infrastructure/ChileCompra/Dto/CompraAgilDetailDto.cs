using System.Text.Json.Serialization;

namespace Ppip.Procurement.Infrastructure.ChileCompra.Dto;

// Basado en la Guía de Uso API Compra Ágil v2 (v3.0) §6.3-6.6, mas
// correcciones del spike real de FASE 5:
// - IdOrdenCompra se observó como campo RAÍZ del payload ("id_orden_compra"),
//   no anidado bajo un objeto "orden_compra.*" como documenta la guía. El
//   ejemplo real capturado no tenía OC emitida (null), así que no se pudo
//   confirmar si id_oc/codigo_orden_compra/estado_orden_compra existen en
//   algún lado cuando sí hay OC — la propia guía admite en su changelog
//   ("corrección de campos orden_compra") que esta parte cambió entre
//   versiones. Queda para FASE 6 confirmar con un ejemplo en estado
//   proveedor_seleccionado antes de construir el anti-corruption layer.
// - proveedores_cotizando[]: el ejemplo real capturado vino vacío ([]) — la
//   forma de sus campos es la documentada en §6.5, SIN verificar contra una
//   respuesta real no vacía.

public sealed class CompraAgilDetailDto
{
    [JsonPropertyName("codigo")]
    public string Codigo { get; init; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; init; }

    [JsonPropertyName("estado")]
    public EstadoDto Estado { get; init; } = new();

    [JsonPropertyName("convocatoria")]
    public ConvocatoriaDetailDto Convocatoria { get; init; } = new();

    [JsonPropertyName("fechas")]
    public FechasDetailDto Fechas { get; init; } = new();

    [JsonPropertyName("entrega")]
    public EntregaDto? Entrega { get; init; }

    [JsonPropertyName("documentos")]
    public IReadOnlyList<DocumentoRefDto> Documentos { get; init; } = [];

    [JsonPropertyName("presupuesto")]
    public PresupuestoDto Presupuesto { get; init; } = new();

    /// <summary>
    /// Distinto de null ⇒ se emitió Orden de Compra (OQ-10: la API sí expone
    /// esta señal). Observado como campo raíz — ver nota de la clase.
    /// </summary>
    [JsonPropertyName("id_orden_compra")]
    public long? IdOrdenCompra { get; init; }

    [JsonPropertyName("institucion")]
    public InstitucionDto Institucion { get; init; } = new();

    [JsonPropertyName("productos_solicitados")]
    public IReadOnlyList<ProductoSolicitadoDto> ProductosSolicitados { get; init; } = [];

    [JsonPropertyName("proveedores_cotizando")]
    public IReadOnlyList<ProveedorCotizandoDto> ProveedoresCotizando { get; init; } = [];

    [JsonPropertyName("resumen")]
    public ResumenDetailDto Resumen { get; init; } = new();

    [JsonPropertyName("motivos")]
    public MotivosDetailDto Motivos { get; init; } = new();

    [JsonPropertyName("flags")]
    public FlagsDto? Flags { get; init; }
}

public sealed class ConvocatoriaDetailDto
{
    [JsonPropertyName("estado_convocatoria")]
    public int EstadoConvocatoria { get; init; }

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>Formato corto observado ("2026-08-18 08:30"), no ISO-8601 — ver nota de fechas en CompraAgilListItemDto.</summary>
    [JsonPropertyName("fecha_cierre_primer_llamado")]
    public string? FechaCierrePrimerLlamado { get; init; }

    [JsonPropertyName("fecha_cierre_segundo_llamado")]
    public string? FechaCierreSegundoLlamado { get; init; }
}

public sealed class FechasDetailDto
{
    [JsonPropertyName("fecha_publicacion")]
    public string? FechaPublicacion { get; init; }

    [JsonPropertyName("fecha_cierre")]
    public string? FechaCierre { get; init; }

    [JsonPropertyName("fecha_ultimo_cambio")]
    public string? FechaUltimoCambio { get; init; }

    [JsonPropertyName("fecha_cancelacion")]
    public string? FechaCancelacion { get; init; }
}

public sealed class EntregaDto
{
    [JsonPropertyName("direccion_entrega")]
    public string? DireccionEntrega { get; init; }

    [JsonPropertyName("plazo_entrega_dias")]
    public int? PlazoEntregaDias { get; init; }
}

public sealed class PresupuestoDto
{
    [JsonPropertyName("tipo_presupuesto")]
    public string? TipoPresupuesto { get; init; }

    [JsonPropertyName("moneda")]
    public string? Moneda { get; init; }

    [JsonPropertyName("presupuesto_estimado")]
    public decimal? PresupuestoEstimado { get; init; }

    [JsonPropertyName("monto_disponible")]
    public decimal? MontoDisponible { get; init; }

    [JsonPropertyName("monto_disponible_clp")]
    public decimal? MontoDisponibleClp { get; init; }

    [JsonPropertyName("valor_cambio_moneda")]
    public decimal? ValorCambioMoneda { get; init; }

    [JsonPropertyName("fecha_cambio_moneda")]
    public string? FechaCambioMoneda { get; init; }
}

public sealed class ProductoSolicitadoDto
{
    // Documentado como int|string mixto (§6.4) — confirmado numérico en la
    // respuesta real capturada. Ver FlexibleStringConverter.
    [JsonPropertyName("codigo_producto")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CodigoProducto { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; init; }

    [JsonPropertyName("cantidad")]
    public decimal Cantidad { get; init; }

    [JsonPropertyName("unidad_medida")]
    public string? UnidadMedida { get; init; }
}

/// <summary>
/// Forma documentada en §6.5 — NO confirmada contra una respuesta real no
/// vacía (el ejemplo capturado en el spike no tenía cotizaciones). Confirmar
/// antes de depender de estos campos para lógica de negocio (FASE 6+).
/// </summary>
public sealed class ProveedorCotizandoDto
{
    [JsonPropertyName("rut_proveedor")]
    public string? RutProveedor { get; init; }

    [JsonPropertyName("razon_social")]
    public string? RazonSocial { get; init; }

    [JsonPropertyName("es_emt")]
    public bool? EsEmt { get; init; }

    [JsonPropertyName("id_cotizacion")]
    public long? IdCotizacion { get; init; }

    [JsonPropertyName("activo")]
    public bool? Activo { get; init; }

    [JsonPropertyName("fecha_creacion")]
    public string? FechaCreacion { get; init; }

    [JsonPropertyName("fecha_vigencia")]
    public string? FechaVigencia { get; init; }

    [JsonPropertyName("valor_neto")]
    public decimal? ValorNeto { get; init; }

    [JsonPropertyName("total_impuesto")]
    public decimal? TotalImpuesto { get; init; }

    [JsonPropertyName("monto_despacho")]
    public decimal? MontoDespacho { get; init; }

    [JsonPropertyName("monto_total")]
    public decimal? MontoTotal { get; init; }

    [JsonPropertyName("descripcion_cotizacion")]
    public string? DescripcionCotizacion { get; init; }

    [JsonPropertyName("justificacion_inadmisibilidad")]
    public string? JustificacionInadmisibilidad { get; init; }

    [JsonPropertyName("productos_cotizados")]
    public IReadOnlyList<ProductoCotizadoDto> ProductosCotizados { get; init; } = [];
}

public sealed class ProductoCotizadoDto
{
    // Misma familia de campo que ProductoSolicitadoDto.CodigoProducto —
    // aplicado defensivamente aunque no verificado (proveedores_cotizando
    // vino vacío en el ejemplo capturado, ver README de fixtures).
    [JsonPropertyName("codigo_producto")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CodigoProducto { get; init; }

    [JsonPropertyName("nombre_producto")]
    public string? NombreProducto { get; init; }

    [JsonPropertyName("cantidad")]
    public decimal? Cantidad { get; init; }

    [JsonPropertyName("precio_unitario")]
    public decimal? PrecioUnitario { get; init; }

    [JsonPropertyName("monto_total_producto")]
    public decimal? MontoTotalProducto { get; init; }
}

public sealed class ResumenDetailDto
{
    [JsonPropertyName("multa_sancion")]
    public decimal? MultaSancion { get; init; }

    [JsonPropertyName("total_ofertas_recibidas")]
    public int TotalOfertasRecibidas { get; init; }

    [JsonPropertyName("total_demandas")]
    public int TotalDemandas { get; init; }
}

public sealed class MotivosDetailDto
{
    [JsonPropertyName("motivo_cancelacion")]
    public string? MotivoCancelacion { get; init; }

    [JsonPropertyName("motivo_desierta")]
    public string? MotivoDesierta { get; init; }
}

public sealed class FlagsDto
{
    [JsonPropertyName("considera_requisitos_medioambientales")]
    public bool ConsideraRequisitosMedioambientales { get; init; }

    [JsonPropertyName("considera_requisitos_impacto_social_economico")]
    public bool ConsideraRequisitosImpactoSocialEconomico { get; init; }
}
