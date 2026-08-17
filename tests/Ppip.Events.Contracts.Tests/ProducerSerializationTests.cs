using System.Text.Json;
using Json.Schema;
using Ppip.BuildingBlocks.Messaging;
using Ppip.Procurement.Application.Events;
using Xunit;

namespace Ppip.Events.Contracts.Tests;

/// <summary>
/// A diferencia de <see cref="CompraAgilEventSchemaTests"/> (valida JSON de
/// ejemplo escrito a mano), esto serializa el envelope tal como lo hace el
/// productor real (<c>Ppip.Procurement.Application.ProcurementEventPublisher</c>,
/// FASE 6) — habría detectado, por ejemplo, que <c>EventEnvelope&lt;T&gt;</c>
/// serializaba en PascalCase por defecto en vez del camelCase que el schema
/// exige (bug real encontrado al construir esto, corregido con
/// <c>[JsonPropertyName]</c> en el envelope y en los payloads).
/// </summary>
public class ProducerSerializationTests
{
    private static readonly JsonSchema EnvelopeSchema = Schemas.Envelope;
    private static readonly JsonSchema CompraAgilDetectedSchema = Schemas.CompraAgilDetectedV1;
    private static readonly JsonSchema CompraAgilUpdatedSchema = Schemas.CompraAgilUpdatedV1;

    [Fact]
    public void RealCompraAgilDetectedEnvelope_SerializedByProducer_MatchesSchema()
    {
        var payload = new CompraAgilDetectedPayload(
            CompraAgilId: "1234-56-COT26",
            Codigo: "1234-56-COT26",
            Nombre: "Adquisición de insumos de laboratorio",
            OrganismoCodigo: "6945",
            FechaCierre: DateTimeOffset.Parse("2026-08-22T15:00:00Z"),
            MontoDisponible: new MoneyPayload(4_500_000m, "CLP"),
            RawPayloadId: Guid.CreateVersion7().ToString(),
            DocumentRefs: []);

        var envelope = EventEnvelope<CompraAgilDetectedPayload>.Create(
            "CompraAgilDetected", 1, "sync-2026-08-16-0930-abc", "sync-worker@1.0.0", payload);

        var json = JsonSerializer.SerializeToDocument(envelope);

        AssertValid(EnvelopeSchema, json);
        AssertValid(CompraAgilDetectedSchema, json);
    }

    [Fact]
    public void RealCompraAgilUpdatedEnvelope_SerializedByProducer_MatchesSchema()
    {
        var payload = new CompraAgilUpdatedPayload(
            CompraAgilId: "1234-56-COT26",
            Version: 2,
            ChangedFields: ["Estado"],
            RawPayloadId: Guid.CreateVersion7().ToString());

        var envelope = EventEnvelope<CompraAgilUpdatedPayload>.Create(
            "CompraAgilUpdated", 1, "sync-2026-08-16-0930-abc", "sync-worker@1.0.0", payload);

        var json = JsonSerializer.SerializeToDocument(envelope);

        AssertValid(EnvelopeSchema, json);
        AssertValid(CompraAgilUpdatedSchema, json);
    }

    private static void AssertValid(JsonSchema schema, JsonDocument instance)
    {
        var result = schema.Evaluate(instance.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (result.IsValid)
        {
            return;
        }

        var failures = result.Details
            .Where(d => !d.IsValid)
            .Select(d => d.Errors is null ? d.EvaluationPath.ToString() : $"{d.EvaluationPath}: {string.Join(',', d.Errors.Values)}");
        Assert.Fail(string.Join("; ", failures));
    }
}
