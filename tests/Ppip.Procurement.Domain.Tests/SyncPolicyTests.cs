using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Procurement.Domain.Tests;

public class SyncPolicyTests
{
    private static CompraAgil ExistingWithHash(string hash) =>
        CompraAgil.Detect(
            CompraAgilId.From("4321-5-LE24"),
            InstitutionRef.From("742", "Municipalidad de Ejemplo"),
            "Compra de notebooks",
            Money.From(1_000_000, "CLP"),
            DateRange.From(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5)),
            hash,
            [],
            "corr-1");

    [Fact]
    public void Decide_NoExistingRecord_ReturnsCreate()
    {
        Assert.Equal(SyncDecision.Create, SyncPolicy.Decide(existing: null, incomingPayloadHash: "hash-1"));
    }

    [Fact]
    public void Decide_SameHash_ReturnsNoOp()
    {
        var existing = ExistingWithHash("hash-1");

        Assert.Equal(SyncDecision.NoOp, SyncPolicy.Decide(existing, "hash-1"));
    }

    [Fact]
    public void Decide_DifferentHash_ReturnsUpdate()
    {
        var existing = ExistingWithHash("hash-1");

        Assert.Equal(SyncDecision.Update, SyncPolicy.Decide(existing, "hash-2"));
    }

    [Fact]
    public void Decide_EmptyIncomingHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => SyncPolicy.Decide(null, ""));
    }
}
