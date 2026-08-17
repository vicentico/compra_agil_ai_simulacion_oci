using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Procurement.Domain.Tests;

public class ValueObjectValidationTests
{
    [Fact]
    public void CompraAgilId_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => CompraAgilId.From(""));
    }

    [Fact]
    public void CompraAgilId_TrimsWhitespace()
    {
        Assert.Equal("4321-5-LE24", CompraAgilId.From("  4321-5-LE24  ").Value);
    }

    [Fact]
    public void Money_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.From(-1, "CLP"));
    }

    [Fact]
    public void Money_EmptyCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => Money.From(100, ""));
    }

    [Fact]
    public void Money_NormalizesCurrencyToUpperInvariant()
    {
        Assert.Equal("CLP", Money.From(100, "clp").Currency);
    }

    [Fact]
    public void Money_SameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(Money.From(100, "CLP"), Money.From(100, "CLP"));
    }

    [Fact]
    public void DateRange_CierreAntesDePublicacion_Throws()
    {
        var publicacion = DateTimeOffset.UtcNow;
        var cierre = publicacion.AddDays(-1);

        Assert.Throws<ArgumentException>(() => DateRange.From(publicacion, cierre));
    }

    [Fact]
    public void DateRange_IsOpenAt_WithinRange_ReturnsTrue()
    {
        var publicacion = DateTimeOffset.UtcNow;
        var cierre = publicacion.AddDays(5);
        var range = DateRange.From(publicacion, cierre);

        Assert.True(range.IsOpenAt(publicacion.AddDays(2)));
    }

    [Fact]
    public void DateRange_IsOpenAt_AfterCierre_ReturnsFalse()
    {
        var publicacion = DateTimeOffset.UtcNow;
        var cierre = publicacion.AddDays(5);
        var range = DateRange.From(publicacion, cierre);

        Assert.False(range.IsOpenAt(publicacion.AddDays(6)));
    }

    [Fact]
    public void ProductRequirement_ZeroQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductRequirement.Create("Notebook", 0, "unidad"));
    }

    [Fact]
    public void ProductRequirement_EmptyProduct_Throws()
    {
        Assert.Throws<ArgumentException>(() => ProductRequirement.Create("", 1, "unidad"));
    }
}
