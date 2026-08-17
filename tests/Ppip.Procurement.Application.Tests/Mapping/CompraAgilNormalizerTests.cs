using Ppip.Procurement.Application.Mapping;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Infrastructure.ChileCompra.Dto;
using Xunit;

namespace Ppip.Procurement.Application.Tests.Mapping;

public class CompraAgilNormalizerTests
{
    private static CompraAgilListItemDto ValidDto(
        string codigo = "418-1191-COT26",
        string estadoCodigo = "publicada",
        string? fechaPublicacion = "2026-08-16 23:02",
        string? fechaCierre = "2026-08-18 08:30") => new()
    {
        Codigo = codigo,
        Nombre = "KCR-OLOPATADINA 0,2% COLIRIO",
        Estado = new EstadoDto { IdEstado = 2, Codigo = estadoCodigo, Glosa = estadoCodigo },
        Fechas = new FechasListDto { FechaPublicacion = fechaPublicacion, FechaCierre = fechaCierre },
        Montos = new MontosDto { Moneda = "CLP", MontoDisponible = 2_000_000m },
        Institucion = new InstitucionDto { OrganismoComprador = "HOSPITAL DE EJEMPLO", Rut = "61.602.279-2" },
    };

    [Fact]
    public void Normalize_ValidItem_Succeeds()
    {
        var result = CompraAgilNormalizer.Normalize(ValidDto());

        Assert.True(result.Success);
        Assert.Equal("418-1191-COT26", result.Id!.Value);
        Assert.Equal("61.602.279-2", result.Institution!.Id);
        Assert.Equal("KCR-OLOPATADINA 0,2% COLIRIO", result.Titulo);
        Assert.Equal(2_000_000m, result.MontoEstimado!.Amount);
        Assert.Equal("CLP", result.MontoEstimado.Currency);
        Assert.Equal(EstadoCompra.Publicada, result.Estado);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("cerrada", EstadoCompra.Cerrada)]
    [InlineData("desierta", EstadoCompra.Desierta)]
    [InlineData("proveedor_seleccionado", EstadoCompra.Adjudicada)]
    [InlineData("PUBLICADA", EstadoCompra.Publicada)]
    public void Normalize_MapsKnownEstadoCodigos(string estadoCodigo, EstadoCompra esperado)
    {
        var result = CompraAgilNormalizer.Normalize(ValidDto(estadoCodigo: estadoCodigo));

        Assert.True(result.Success);
        Assert.Equal(esperado, result.Estado);
    }

    [Fact]
    public void Normalize_UnknownEstadoCodigo_Quarantines()
    {
        // "cancelada" existe en la API real (EstadoDto.Codigo) pero
        // EstadoCompra (FASE 4) no la modela todavía — ver comentario en
        // CompraAgilNormalizer.
        var result = CompraAgilNormalizer.Normalize(ValidDto(estadoCodigo: "cancelada"));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("cancelada"));
    }

    [Fact]
    public void Normalize_MissingFechaPublicacion_Quarantines()
    {
        var result = CompraAgilNormalizer.Normalize(ValidDto(fechaPublicacion: null));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("fechas"));
    }

    [Fact]
    public void Normalize_UnrecognizedDateFormat_Quarantines()
    {
        var result = CompraAgilNormalizer.Normalize(ValidDto(fechaCierre: "16/08/2026"));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("fechas"));
    }

    [Fact]
    public void Normalize_MissingCodigo_Quarantines()
    {
        var result = CompraAgilNormalizer.Normalize(ValidDto(codigo: ""));

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("codigo"));
    }

    [Fact]
    public void Normalize_MontoDisponibleNull_FallsBackToMontoDisponibleClp()
    {
        var dto = ValidDto();
        var conFallback = new CompraAgilListItemDto
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Estado = dto.Estado,
            Fechas = dto.Fechas,
            Institucion = dto.Institucion,
            Montos = new MontosDto { Moneda = "CLP", MontoDisponible = null, MontoDisponibleClp = 750_000m },
        };

        var result = CompraAgilNormalizer.Normalize(conFallback);

        Assert.True(result.Success);
        Assert.Equal(750_000m, result.MontoEstimado!.Amount);
    }
}
