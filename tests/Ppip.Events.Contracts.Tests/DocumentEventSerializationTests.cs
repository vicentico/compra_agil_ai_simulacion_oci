using System.Text.Json;
using Json.Schema;
using Ppip.BuildingBlocks.Messaging;
using Ppip.DocumentIntelligence.Application.Events;
using Xunit;

namespace Ppip.Events.Contracts.Tests;

/// <summary>
/// Serializa con el código real del productor
/// (<c>Ppip.DocumentIntelligence.Application.DocumentEventPublisher</c>,
/// FASE 7-8), no JSON escrito a mano — mismo criterio que
/// <see cref="ProducerSerializationTests"/> (que encontró un bug real de
/// serialización en FASE 6). Cierra retroactivamente el hueco de FASE 7:
/// <c>DocumentDetected.v1</c>/<c>DocumentDownloaded.v1</c> tenían productor
/// real desde entonces pero nunca tuvieron schema ni este tipo de test.
/// </summary>
public class DocumentEventSerializationTests
{
    private static readonly JsonSchema EnvelopeSchema = Schemas.Envelope;

    [Fact]
    public void RealDocumentDetectedEnvelope_SerializedByProducer_MatchesSchema()
    {
        var payload = new DocumentDetectedPayload("018f3c1e-0000-7000-8000-000000000001", "1234-56-COT26", "https://docs.mercadopublico.cl/bases.pdf", "bases.pdf");
        var envelope = EventEnvelope<DocumentDetectedPayload>.Create("DocumentDetected", 1, "doc-corr-1", "document-worker@1.0.0", payload);

        var json = JsonSerializer.SerializeToDocument(envelope);

        SchemaAssertions.AssertValid(EnvelopeSchema, json);
        SchemaAssertions.AssertValid(Schemas.DocumentDetectedV1, json);
    }

    [Fact]
    public void RealDocumentDownloadedEnvelope_SerializedByProducer_MatchesSchema()
    {
        var payload = new DocumentDownloadedPayload(
            DocumentId: "018f3c1e-0000-7000-8000-000000000001",
            CompraAgilId: "1234-56-COT26",
            VersionId: "018f3c1e-0000-7000-8000-000000000002",
            Sha256: new string('a', 64),
            StorageRef: new StorageRefPayload("chilecompra", "1234-56-COT26/original/bases.pdf"),
            SizeBytes: 204800);
        var envelope = EventEnvelope<DocumentDownloadedPayload>.Create("DocumentDownloaded", 1, "doc-corr-1", "document-worker@1.0.0", payload);

        var json = JsonSerializer.SerializeToDocument(envelope);

        SchemaAssertions.AssertValid(EnvelopeSchema, json);
        SchemaAssertions.AssertValid(Schemas.DocumentDownloadedV1, json);
    }

    [Fact]
    public void RealDocumentExtractedEnvelope_SerializedByProducer_MatchesSchema()
    {
        var payload = new DocumentExtractedPayload("018f3c1e-0000-7000-8000-000000000001", "018f3c1e-0000-7000-8000-000000000002", Pages: 5, Classification: "Textual", TextDensity: 0.012);
        var envelope = EventEnvelope<DocumentExtractedPayload>.Create("DocumentExtracted", 1, "doc-corr-1", "document-worker@1.0.0", payload);

        var json = JsonSerializer.SerializeToDocument(envelope);

        SchemaAssertions.AssertValid(EnvelopeSchema, json);
        SchemaAssertions.AssertValid(Schemas.DocumentExtractedV1, json);
    }

    [Fact]
    public void RealOcrCompletedEnvelope_SerializedByProducer_MatchesSchema()
    {
        var payload = new OcrCompletedPayload("018f3c1e-0000-7000-8000-000000000001", "018f3c1e-0000-7000-8000-000000000002", PagesOcr: [2, 3], AvgConfidence: 0.87);
        var envelope = EventEnvelope<OcrCompletedPayload>.Create("OcrCompleted", 1, "doc-corr-1", "document-worker@1.0.0", payload);

        var json = JsonSerializer.SerializeToDocument(envelope);

        SchemaAssertions.AssertValid(EnvelopeSchema, json);
        SchemaAssertions.AssertValid(Schemas.OcrCompletedV1, json);
    }

    [Fact]
    public void RealDocumentChunkedEnvelope_SerializedByProducer_MatchesSchema()
    {
        var payload = new DocumentChunkedPayload("018f3c1e-0000-7000-8000-000000000001", "018f3c1e-0000-7000-8000-000000000002", ChunkCount: 3, ChunkIds: ["a", "b", "c"]);
        var envelope = EventEnvelope<DocumentChunkedPayload>.Create("DocumentChunked", 1, "doc-corr-1", "document-worker@1.0.0", payload);

        var json = JsonSerializer.SerializeToDocument(envelope);

        SchemaAssertions.AssertValid(EnvelopeSchema, json);
        SchemaAssertions.AssertValid(Schemas.DocumentChunkedV1, json);
    }
}
