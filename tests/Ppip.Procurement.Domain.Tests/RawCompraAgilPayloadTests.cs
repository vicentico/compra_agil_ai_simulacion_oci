using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Procurement.Domain.Tests;

public class RawCompraAgilPayloadTests
{
    [Fact]
    public void Capture_SamePayload_ProducesSameHash()
    {
        var a = RawCompraAgilPayload.Capture("{\"codigo\":\"1\"}", "https://api/v2/x", DateTimeOffset.UtcNow, 200, "v2", "corr-1");
        var b = RawCompraAgilPayload.Capture("{\"codigo\":\"1\"}", "https://api/v2/x", DateTimeOffset.UtcNow, 200, "v2", "corr-2");

        Assert.Equal(a.ResponseHash, b.ResponseHash);
    }

    [Fact]
    public void Capture_DifferentPayload_ProducesDifferentHash()
    {
        var a = RawCompraAgilPayload.Capture("{\"codigo\":\"1\"}", "https://api/v2/x", DateTimeOffset.UtcNow, 200, "v2", "corr-1");
        var b = RawCompraAgilPayload.Capture("{\"codigo\":\"2\"}", "https://api/v2/x", DateTimeOffset.UtcNow, 200, "v2", "corr-1");

        Assert.NotEqual(a.ResponseHash, b.ResponseHash);
    }

    [Fact]
    public void Capture_EmptyPayload_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RawCompraAgilPayload.Capture("", "https://api/v2/x", DateTimeOffset.UtcNow, 200, "v2", "corr-1"));
    }
}
