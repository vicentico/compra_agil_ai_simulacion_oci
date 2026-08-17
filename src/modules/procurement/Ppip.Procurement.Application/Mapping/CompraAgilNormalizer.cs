using Ppip.Procurement.Domain;
using Ppip.Procurement.Infrastructure.ChileCompra;
using Ppip.Procurement.Infrastructure.ChileCompra.Dto;

namespace Ppip.Procurement.Application.Mapping;

/// <summary>
/// Traduce <see cref="CompraAgilListItemDto"/> (contrato crudo de ChileCompra,
/// FASE 5) a los value objects normalizados de <c>Ppip.Procurement.Domain</c>
/// (UC-001 paso 5). Puro: sin acceso a infraestructura ni al agregado
/// existente — esa comparación la hace <see cref="SyncPolicy"/> en el
/// orquestador.
/// </summary>
public static class CompraAgilNormalizer
{
    // "cancelada" y otros valores no documentados/no vistos en el spike de
    // FASE 5 quedan fuera a propósito: EstadoCompra (FASE 4) no los modela
    // todavía (su máquina de estados es publicada→cerrada→adjudicada/desierta).
    // Forzar un mapeo ahora sería adivinar una semántica no confirmada contra
    // la API real — se prefiere poner el registro en cuarentena (F3,
    // docs/14-reliability/01) hasta que un caso real lo confirme y se
    // extienda el modelo de dominio deliberadamente.
    private static readonly IReadOnlyDictionary<string, EstadoCompra> EstadoMap = new Dictionary<string, EstadoCompra>(StringComparer.OrdinalIgnoreCase)
    {
        ["publicada"] = EstadoCompra.Publicada,
        ["cerrada"] = EstadoCompra.Cerrada,
        ["desierta"] = EstadoCompra.Desierta,
        ["proveedor_seleccionado"] = EstadoCompra.Adjudicada,
    };

    public static NormalizationResult Normalize(CompraAgilListItemDto dto)
    {
        var errors = new List<string>();

        CompraAgilId? id = null;
        try
        {
            id = CompraAgilId.From(dto.Codigo);
        }
        catch (ArgumentException ex)
        {
            errors.Add($"codigo: {ex.Message}");
        }

        InstitutionRef? institution = null;
        try
        {
            institution = InstitutionRef.From(dto.Institucion.Rut, dto.Institucion.OrganismoComprador);
        }
        catch (ArgumentException ex)
        {
            errors.Add($"institucion: {ex.Message}");
        }

        var titulo = dto.Nombre?.Trim();
        if (string.IsNullOrWhiteSpace(titulo))
        {
            errors.Add("nombre: vacío.");
            titulo = null;
        }

        Money? monto = null;
        try
        {
            var amount = dto.Montos.MontoDisponible ?? dto.Montos.MontoDisponibleClp ?? 0m;
            var currency = string.IsNullOrWhiteSpace(dto.Montos.Moneda) ? "CLP" : dto.Montos.Moneda;
            monto = Money.From(amount, currency);
        }
        catch (ArgumentException ex)
        {
            errors.Add($"montos: {ex.Message}");
        }

        var publicacion = ChileCompraDateParser.TryParse(dto.Fechas.FechaPublicacion);
        var cierre = ChileCompraDateParser.TryParse(dto.Fechas.FechaCierre);
        DateRange? vigencia = null;
        if (publicacion is null || cierre is null)
        {
            errors.Add("fechas: fecha_publicacion/fecha_cierre ausentes o en un formato no reconocido.");
        }
        else
        {
            try
            {
                vigencia = DateRange.From(publicacion.Value, cierre.Value);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"fechas: {ex.Message}");
            }
        }

        EstadoCompra? estado = null;
        if (EstadoMap.TryGetValue(dto.Estado.Codigo, out var mappedEstado))
        {
            estado = mappedEstado;
        }
        else
        {
            errors.Add($"estado.codigo '{dto.Estado.Codigo}' no reconocido — EstadoCompra no lo modela todavía.");
        }

        if (errors.Count > 0 || id is null || institution is null || titulo is null || monto is null || vigencia is null || estado is null)
        {
            return NormalizationResult.Failed(errors);
        }

        return NormalizationResult.Ok(id, institution, titulo, monto, vigencia, estado.Value);
    }
}

/// <summary>Resultado de <see cref="CompraAgilNormalizer.Normalize"/>: éxito con todos los campos, o fallo con los motivos (F3, registro en cuarentena).</summary>
public sealed record NormalizationResult(
    bool Success,
    CompraAgilId? Id,
    InstitutionRef? Institution,
    string? Titulo,
    Money? MontoEstimado,
    DateRange? Vigencia,
    EstadoCompra? Estado,
    IReadOnlyList<string> Errors)
{
    public static NormalizationResult Ok(CompraAgilId id, InstitutionRef institution, string titulo, Money montoEstimado, DateRange vigencia, EstadoCompra estado) =>
        new(true, id, institution, titulo, montoEstimado, vigencia, estado, []);

    public static NormalizationResult Failed(IReadOnlyList<string> errors) =>
        new(false, null, null, null, null, null, null, errors);
}
