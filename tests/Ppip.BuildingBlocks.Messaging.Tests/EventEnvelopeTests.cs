using Ppip.BuildingBlocks.Messaging;
using Xunit;

namespace Ppip.BuildingBlocks.Messaging.Tests;

public class EventEnvelopeTests
{
    private sealed record CompraAgilDetectedPayload(string CompraAgilId);

    [Fact]
    public void Create_GeneratesVersion7EventId()
    {
        var envelope = EventEnvelope<CompraAgilDetectedPayload>.Create(
            eventType: "CompraAgilDetected",
            version: 1,
            correlationId: "corr-1",
            producer: "sync-worker@1.0.0",
            payload: new CompraAgilDetectedPayload("4321-5-LE24"));

        Assert.Equal(7, envelope.EventId.Version);
    }

    [Fact]
    public void Create_SetsTimestampToNow()
    {
        var before = DateTimeOffset.UtcNow;

        var envelope = EventEnvelope<CompraAgilDetectedPayload>.Create(
            "CompraAgilDetected", 1, "corr-1", "sync-worker@1.0.0", new CompraAgilDetectedPayload("id"));

        var after = DateTimeOffset.UtcNow;
        Assert.InRange(envelope.Timestamp, before, after);
    }

    [Theory]
    [InlineData("", "corr-1", "sync-worker@1.0.0")]
    [InlineData("CompraAgilDetected", "", "sync-worker@1.0.0")]
    [InlineData("CompraAgilDetected", "corr-1", "")]
    public void Create_RequiresEventTypeCorrelationIdAndProducer(string eventType, string correlationId, string producer)
    {
        Assert.Throws<ArgumentException>(() =>
            EventEnvelope<CompraAgilDetectedPayload>.Create(
                eventType, 1, correlationId, producer, new CompraAgilDetectedPayload("id")));
    }

    [Fact]
    public void RoutingKey_MatchesNormativeExample()
    {
        // docs/07-events/01-example-compra-agil-detected.md
        var envelope = EventEnvelope<CompraAgilDetectedPayload>.Create(
            "CompraAgilDetected", 1, "corr-1", "sync-worker@1.0.0", new CompraAgilDetectedPayload("id"));

        Assert.Equal("procurement.compra-agil-detected.v1", envelope.RoutingKey("procurement"));
    }
}
