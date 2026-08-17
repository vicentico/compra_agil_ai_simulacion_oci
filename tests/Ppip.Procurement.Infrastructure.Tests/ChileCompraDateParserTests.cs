using Ppip.Procurement.Infrastructure.ChileCompra;
using Xunit;

namespace Ppip.Procurement.Infrastructure.Tests;

public class ChileCompraDateParserTests
{
    [Fact]
    public void TryParse_IsoWithMilliseconds_ParsesAsUtc()
    {
        // Formato real observado en fechas.fecha_ultimo_cambio del listado.
        var result = ChileCompraDateParser.TryParse("2026-08-16T23:05:02.410Z");

        Assert.NotNull(result);
        Assert.Equal(TimeSpan.Zero, result.Value.Offset);
        Assert.Equal(23, result.Value.Hour);
    }

    [Fact]
    public void TryParse_ShortFormatWithoutTimezone_AssumesChileTime()
    {
        // Formato real observado en fechas.fecha_publicacion (mismo objeto,
        // mismo endpoint, formato distinto — hallazgo del spike de FASE 5).
        var result = ChileCompraDateParser.TryParse("2026-08-16 23:02");

        Assert.NotNull(result);
        Assert.Equal(23, result.Value.Hour);
        Assert.Equal(2, result.Value.Minute);
    }

    [Fact]
    public void TryParse_Null_ReturnsNull()
    {
        Assert.Null(ChileCompraDateParser.TryParse(null));
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsNull()
    {
        Assert.Null(ChileCompraDateParser.TryParse(""));
    }

    [Fact]
    public void TryParse_UnrecognizedFormat_ReturnsNull()
    {
        Assert.Null(ChileCompraDateParser.TryParse("not-a-date"));
    }
}
