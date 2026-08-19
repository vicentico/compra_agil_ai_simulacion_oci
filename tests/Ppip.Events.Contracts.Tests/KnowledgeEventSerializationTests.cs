using System.Text.Json;
using Json.Schema;
using Ppip.BuildingBlocks.Messaging;
using Ppip.Knowledge.Application.Events;
using Xunit;

namespace Ppip.Events.Contracts.Tests;

/// <summary>
/// Serializa con el código real del productor
/// (<c>Ppip.Knowledge.Application.KnowledgeEventPublisher</c>, FASE 9), no
/// JSON escrito a mano — mismo criterio que <see cref="DocumentEventSerializationTests"/>.
/// </summary>
public class KnowledgeEventSerializationTests
{
    [Fact]
    public void RealEmbeddingCreatedEnvelope_SerializedByProducer_MatchesSchema()
    {
        var payload = new EmbeddingCreatedPayload(
            DocumentId: "018f3c1e-0000-7000-8000-000000000001",
            VersionId: "018f3c1e-0000-7000-8000-000000000002",
            ModelVersion: "nomic-embed-text",
            IndexedCount: 12,
            IsLastOfCompra: true);
        var envelope = EventEnvelope<EmbeddingCreatedPayload>.Create("EmbeddingCreated", 1, "doc-corr-1", "document-worker@1.0.0", payload);

        var json = JsonSerializer.SerializeToDocument(envelope);

        SchemaAssertions.AssertValid(Schemas.Envelope, json);
        SchemaAssertions.AssertValid(Schemas.EmbeddingCreatedV1, json);
    }
}
