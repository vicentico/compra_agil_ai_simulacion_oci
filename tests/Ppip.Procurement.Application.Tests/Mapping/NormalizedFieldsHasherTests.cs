using Ppip.Procurement.Application.Mapping;
using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Procurement.Application.Tests.Mapping;

public class NormalizedFieldsHasherTests
{
    private static readonly Money Monto = Money.From(1_000_000, "CLP");
    private static readonly DateRange Vigencia = DateRange.From(
        new DateTimeOffset(2026, 8, 16, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Compute_SameInputs_IsDeterministic()
    {
        var hash1 = NormalizedFieldsHasher.Compute("Compra de notebooks", Monto, Vigencia, EstadoCompra.Publicada);
        var hash2 = NormalizedFieldsHasher.Compute("Compra de notebooks", Monto, Vigencia, EstadoCompra.Publicada);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Compute_DifferentEstado_ChangesHash()
    {
        // Este es exactamente el caso que motivó el diseño: si solo cambia el
        // estado (p.ej. publicada→cerrada), SyncPolicy debe detectarlo como
        // Update aunque título/monto/vigencia sigan iguales.
        var hashPublicada = NormalizedFieldsHasher.Compute("Compra de notebooks", Monto, Vigencia, EstadoCompra.Publicada);
        var hashCerrada = NormalizedFieldsHasher.Compute("Compra de notebooks", Monto, Vigencia, EstadoCompra.Cerrada);

        Assert.NotEqual(hashPublicada, hashCerrada);
    }

    [Fact]
    public void Compute_DifferentTitulo_ChangesHash()
    {
        var hash1 = NormalizedFieldsHasher.Compute("Compra de notebooks", Monto, Vigencia, EstadoCompra.Publicada);
        var hash2 = NormalizedFieldsHasher.Compute("Compra de monitores", Monto, Vigencia, EstadoCompra.Publicada);

        Assert.NotEqual(hash1, hash2);
    }
}
