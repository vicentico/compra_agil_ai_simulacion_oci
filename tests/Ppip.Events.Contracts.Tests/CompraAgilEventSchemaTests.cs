using System.Text.Json;
using Json.Schema;
using Xunit;

namespace Ppip.Events.Contracts.Tests;

/// <summary>
/// Valida que los ejemplos normativos de docs/07-events/ efectivamente
/// cumplen los JSON Schema versionados en docs/07-events/schemas/ (regla 5,
/// docs/07-events/00-event-conventions.md) — contract test productor↔schema,
/// per docs/15-testing/01-test-strategy.md ("Contract", F4+).
/// </summary>
public class CompraAgilEventSchemaTests
{
    private static readonly JsonSchema CompraAgilDetectedSchema = Schemas.CompraAgilDetectedV1;
    private static readonly JsonSchema CompraAgilUpdatedSchema = Schemas.CompraAgilUpdatedV1;

    [Fact]
    public void CompraAgilDetectedV1_NormativeExample_IsValid()
    {
        // docs/07-events/01-example-compra-agil-detected.md
        var schema = CompraAgilDetectedSchema;
        var instance = JsonDocument.Parse("""
        {
          "eventId": "018f3c1e-1234-7abc-8def-000000000001",
          "eventType": "CompraAgilDetected",
          "version": 1,
          "timestamp": "2026-08-16T09:30:12Z",
          "correlationId": "sync-2026-08-16-0930-abc",
          "causationId": "cmd-sync-cycle-449",
          "producer": "sync-worker@0.1.0",
          "isDemoData": false,
          "payload": {
            "compraAgilId": "1234-56-COT26",
            "codigo": "1234-56-COT26",
            "nombre": "Adquisicion de insumos de laboratorio",
            "organismoCodigo": "6945",
            "fechaCierre": "2026-08-22T15:00:00Z",
            "montoDisponible": { "amount": 4500000, "currency": "CLP" },
            "rawPayloadId": "raw_663d",
            "documentRefs": [
              { "documentId": "doc_9f2c", "sourceUrl": "https://example.org/bases.pdf", "declaredName": "Bases_CompraAgil.pdf" }
            ]
          }
        }
        """);

        var result = schema.Evaluate(instance.RootElement);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CompraAgilDetectedV1_MissingRawPayloadId_IsInvalid()
    {
        var schema = CompraAgilDetectedSchema;
        var instance = JsonDocument.Parse("""
        {
          "eventId": "018f3c1e-1234-7abc-8def-000000000001",
          "eventType": "CompraAgilDetected",
          "version": 1,
          "timestamp": "2026-08-16T09:30:12Z",
          "correlationId": "sync-2026-08-16-0930-abc",
          "producer": "sync-worker@0.1.0",
          "isDemoData": false,
          "payload": {
            "compraAgilId": "1234-56-COT26",
            "codigo": "1234-56-COT26",
            "documentRefs": []
          }
        }
        """);

        var result = schema.Evaluate(instance.RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CompraAgilUpdatedV1_ValidPayload_IsValid()
    {
        var schema = CompraAgilUpdatedSchema;
        var instance = JsonDocument.Parse("""
        {
          "eventId": "018f3c1e-1234-7abc-8def-000000000002",
          "eventType": "CompraAgilUpdated",
          "version": 1,
          "timestamp": "2026-08-16T10:00:00Z",
          "correlationId": "sync-2026-08-16-0930-abc",
          "causationId": null,
          "producer": "sync-worker@0.1.0",
          "isDemoData": false,
          "payload": {
            "compraAgilId": "1234-56-COT26",
            "version": 2,
            "changedFields": ["Titulo"],
            "rawPayloadId": "raw_663e"
          }
        }
        """);

        var result = schema.Evaluate(instance.RootElement);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CompraAgilUpdatedV1_EmptyChangedFields_IsInvalid()
    {
        // "sin cambio no genera escritura" — un evento Updated siempre trae
        // al menos un campo cambiado (docs/07-events/00, UC-001 paso 7).
        var schema = CompraAgilUpdatedSchema;
        var instance = JsonDocument.Parse("""
        {
          "eventId": "018f3c1e-1234-7abc-8def-000000000002",
          "eventType": "CompraAgilUpdated",
          "version": 1,
          "timestamp": "2026-08-16T10:00:00Z",
          "producer": "sync-worker@0.1.0",
          "correlationId": "sync-2026-08-16-0930-abc",
          "isDemoData": false,
          "payload": {
            "compraAgilId": "1234-56-COT26",
            "version": 2,
            "changedFields": [],
            "rawPayloadId": "raw_663e"
          }
        }
        """);

        var result = schema.Evaluate(instance.RootElement);

        Assert.False(result.IsValid);
    }
}
