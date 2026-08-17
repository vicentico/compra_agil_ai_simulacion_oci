using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Procurement.Domain.Tests;

public class CompraAgilTests
{
    private static readonly InstitutionRef Institucion = InstitutionRef.From("742", "Municipalidad de Ejemplo");
    private static readonly DateRange Vigencia = DateRange.From(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5));

    private static CompraAgil DetectDefault(string hash = "hash-v1") =>
        CompraAgil.Detect(
            CompraAgilId.From("4321-5-LE24"),
            Institucion,
            "Compra de notebooks",
            Money.From(1_000_000, "CLP"),
            Vigencia,
            hash,
            [ProductRequirement.Create("Notebook", 10, "unidad")],
            correlationId: "corr-1");

    [Fact]
    public void Detect_StartsAsPublicadaVersion1()
    {
        var compra = DetectDefault();

        Assert.Equal(EstadoCompra.Publicada, compra.Estado);
        Assert.Equal(1, compra.Version);
    }

    [Fact]
    public void Detect_RaisesCompraAgilDetected()
    {
        var compra = DetectDefault(hash: "hash-abc");

        var evento = Assert.Single(compra.DomainEvents);
        var detected = Assert.IsType<CompraAgilDetected>(evento);
        Assert.Equal("4321-5-LE24", detected.CompraAgilId);
        Assert.Equal("hash-abc", detected.RawPayloadHash);
        Assert.Equal("corr-1", detected.CorrelationId);
    }

    [Fact]
    public void ApplyUpdate_SameHash_IsNoOp()
    {
        var compra = DetectDefault(hash: "hash-v1");
        compra.PullDomainEvents();

        compra.ApplyUpdate(
            "Compra de notebooks", Money.From(1_000_000, "CLP"), Vigencia, "hash-v1", compra.Requirements, "corr-2");

        Assert.Equal(1, compra.Version);
        Assert.Empty(compra.DomainEvents);
    }

    [Fact]
    public void ApplyUpdate_DifferentHash_IncrementsVersionAndRaisesEvent()
    {
        var compra = DetectDefault(hash: "hash-v1");
        compra.PullDomainEvents();

        compra.ApplyUpdate(
            "Compra de notebooks y monitores",
            Money.From(1_200_000, "CLP"),
            Vigencia,
            "hash-v2",
            compra.Requirements,
            "corr-2");

        Assert.Equal(2, compra.Version);
        var evento = Assert.Single(compra.DomainEvents);
        var updated = Assert.IsType<CompraAgilUpdated>(evento);
        Assert.Equal(2, updated.Version);
        Assert.Contains(nameof(CompraAgil.Titulo), updated.ChangedFields);
        Assert.Contains(nameof(CompraAgil.MontoEstimado), updated.ChangedFields);
        Assert.DoesNotContain(nameof(CompraAgil.Vigencia), updated.ChangedFields);
    }

    [Fact]
    public void Cerrar_ThenAdjudicar_Succeeds()
    {
        var compra = DetectDefault();

        compra.Cerrar();
        compra.Adjudicar();

        Assert.Equal(EstadoCompra.Adjudicada, compra.Estado);
    }

    [Fact]
    public void Adjudicar_WithoutCerrarFirst_Throws()
    {
        var compra = DetectDefault();

        Assert.Throws<InvalidOperationException>(compra.Adjudicar);
    }

    [Fact]
    public void DeclararDesierta_AfterAdjudicada_Throws()
    {
        var compra = DetectDefault();
        compra.Cerrar();
        compra.Adjudicar();

        Assert.Throws<InvalidOperationException>(compra.DeclararDesierta);
    }
}
