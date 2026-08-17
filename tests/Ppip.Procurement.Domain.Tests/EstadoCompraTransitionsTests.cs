using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Procurement.Domain.Tests;

public class EstadoCompraTransitionsTests
{
    [Theory]
    [InlineData(EstadoCompra.Publicada, EstadoCompra.Cerrada, true)]
    [InlineData(EstadoCompra.Cerrada, EstadoCompra.Adjudicada, true)]
    [InlineData(EstadoCompra.Cerrada, EstadoCompra.Desierta, true)]
    [InlineData(EstadoCompra.Publicada, EstadoCompra.Adjudicada, false)]
    [InlineData(EstadoCompra.Publicada, EstadoCompra.Desierta, false)]
    [InlineData(EstadoCompra.Adjudicada, EstadoCompra.Cerrada, false)]
    [InlineData(EstadoCompra.Desierta, EstadoCompra.Cerrada, false)]
    [InlineData(EstadoCompra.Adjudicada, EstadoCompra.Desierta, false)]
    public void IsValid_MatchesDocumentedTransitions(EstadoCompra from, EstadoCompra to, bool expected)
    {
        Assert.Equal(expected, EstadoCompraTransitions.IsValid(from, to));
    }
}
