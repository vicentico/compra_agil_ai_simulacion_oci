using Ppip.Procurement.Infrastructure.ChileCompra;
using Xunit;

namespace Ppip.Procurement.Infrastructure.Tests;

public class CompraAgilListQueryTests
{
    [Fact]
    public void Validate_TtlAndCambioDesde_Throws()
    {
        var query = new CompraAgilListQuery { TtlCambioMs = 1000, CambioDesde = DateTimeOffset.UtcNow };

        Assert.Throws<ArgumentException>(query.Validate);
    }

    [Fact]
    public void Validate_IdAndQ_Throws()
    {
        var query = new CompraAgilListQuery { Id = "418-1191-COT26", Q = "electricos" };

        Assert.Throws<ArgumentException>(query.Validate);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(51)]
    public void Validate_TamanoPaginaOutOfRange_Throws(int tamanoPagina)
    {
        // Mínimo 10 es un hallazgo del spike (no documentado por ChileCompra).
        var query = new CompraAgilListQuery { TamanoPagina = tamanoPagina };

        Assert.Throws<ArgumentException>(query.Validate);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(50)]
    public void Validate_TamanoPaginaInRange_DoesNotThrow(int tamanoPagina)
    {
        var query = new CompraAgilListQuery { TamanoPagina = tamanoPagina };

        var exception = Record.Exception(query.Validate);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_RegionOutOfRange_Throws()
    {
        var query = new CompraAgilListQuery { TamanoPagina = 15, Region = [17] };

        Assert.Throws<ArgumentException>(query.Validate);
    }

    [Fact]
    public void ToQueryParameters_JoinsEstadoWithComma()
    {
        var query = new CompraAgilListQuery { TamanoPagina = 15, Estado = ["publicada", "proveedor_seleccionado"] };

        var parameters = query.ToQueryParameters();

        Assert.Equal("publicada,proveedor_seleccionado", parameters["estado"]);
    }

    [Fact]
    public void ToQueryParameters_DefaultsIncludePagination()
    {
        var parameters = new CompraAgilListQuery().ToQueryParameters();

        Assert.Equal("15", parameters["tamano_pagina"]);
        Assert.Equal("1", parameters["numero_pagina"]);
    }
}
