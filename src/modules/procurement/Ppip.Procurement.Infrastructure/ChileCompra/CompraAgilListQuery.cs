namespace Ppip.Procurement.Infrastructure.ChileCompra;

public enum OrdenarPor
{
    FechaUltimaModificacion,
    FechaPublicacion,
}

/// <summary>
/// Filtros de <c>GET /v2/compra-agil</c> (Guía de Uso API Compra Ágil v2 §5.1).
/// Valida las mismas reglas que documenta la guía, más una descubierta en el
/// spike de FASE 5 y no documentada: <c>tamano_pagina</c> tiene mínimo 10
/// (no solo máximo 50) — enviar menos devuelve 400.
/// </summary>
public sealed class CompraAgilListQuery
{
    public int? TtlCambioMs { get; init; }
    public DateTimeOffset? CambioDesde { get; init; }
    public DateTimeOffset? CambioHasta { get; init; }
    public DateTimeOffset? PublicadoDesde { get; init; }
    public DateTimeOffset? PublicadoHasta { get; init; }
    public IReadOnlyList<string> Estado { get; init; } = [];
    public IReadOnlyList<int> Region { get; init; } = [];
    public string? Id { get; init; }
    public string? Q { get; init; }
    public int TamanoPagina { get; init; } = 15;
    public int NumeroPagina { get; init; } = 1;
    public OrdenarPor? OrdenarPorCampo { get; init; }

    /// <summary>Lanza <see cref="ArgumentException"/> si los filtros violan las reglas documentadas/descubiertas.</summary>
    public void Validate()
    {
        var usaTtl = TtlCambioMs is not null;
        var usaRango = CambioDesde is not null || CambioHasta is not null;
        if (usaTtl && usaRango)
        {
            throw new ArgumentException("ttl_cambio_ms y cambio_desde/cambio_hasta son mutuamente excluyentes (§5.1 Grupo 1).");
        }

        if (Id is not null && Q is not null)
        {
            throw new ArgumentException("id y q son mutuamente excluyentes (§5.1 Grupo 5).");
        }

        // Mínimo real (10) descubierto en el spike — no documentado, la guía solo indica el máximo (50).
        if (TamanoPagina is < 10 or > 50)
        {
            throw new ArgumentException("tamano_pagina debe estar entre 10 y 50 (mínimo no documentado, hallazgo del spike de FASE 5).");
        }

        if (NumeroPagina < 1)
        {
            throw new ArgumentException("numero_pagina debe ser mayor o igual a 1.");
        }

        foreach (var region in Region)
        {
            if (region is < 1 or > 16)
            {
                throw new ArgumentException($"Código de región inválido: {region} (rango válido 1-16, §5.1 Grupo 4).");
            }
        }
    }

    public IReadOnlyDictionary<string, string> ToQueryParameters()
    {
        Validate();

        var parameters = new Dictionary<string, string>();

        if (TtlCambioMs is { } ttl)
        {
            parameters["ttl_cambio_ms"] = ttl.ToString();
        }

        if (CambioDesde is { } cambioDesde)
        {
            parameters["cambio_desde"] = cambioDesde.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        if (CambioHasta is { } cambioHasta)
        {
            parameters["cambio_hasta"] = cambioHasta.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        if (PublicadoDesde is { } publicadoDesde)
        {
            parameters["publicado_desde"] = publicadoDesde.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        if (PublicadoHasta is { } publicadoHasta)
        {
            parameters["publicado_hasta"] = publicadoHasta.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        if (Estado.Count > 0)
        {
            parameters["estado"] = string.Join(',', Estado);
        }

        if (Region.Count > 0)
        {
            parameters["region"] = string.Join(',', Region);
        }

        if (Id is not null)
        {
            parameters["id"] = Id;
        }

        if (Q is not null)
        {
            parameters["q"] = Q;
        }

        parameters["tamano_pagina"] = TamanoPagina.ToString();
        parameters["numero_pagina"] = NumeroPagina.ToString();

        if (OrdenarPorCampo is { } ordenarPor)
        {
            parameters["ordenar_por"] = ordenarPor.ToString();
        }

        return parameters;
    }
}
