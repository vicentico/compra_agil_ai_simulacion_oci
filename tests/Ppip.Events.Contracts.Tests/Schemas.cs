using Json.Schema;

namespace Ppip.Events.Contracts.Tests;

/// <summary>
/// Carga cada schema exactamente una vez para todo el ensamblado de tests.
/// <c>JsonSchema.FromFile</c> registra el <c>$id</c> en un registro global —
/// cargar el mismo archivo dos veces (p.ej. desde dos clases de test
/// distintas) lanza "Overwriting registered schemas is not permitted".
/// </summary>
internal static class Schemas
{
    private static readonly string SchemasDir = Path.Combine(AppContext.BaseDirectory, "schemas");

    public static readonly JsonSchema Envelope = Load("envelope.schema.json");
    public static readonly JsonSchema CompraAgilDetectedV1 = Load("CompraAgilDetected.v1.schema.json");
    public static readonly JsonSchema CompraAgilUpdatedV1 = Load("CompraAgilUpdated.v1.schema.json");
    public static readonly JsonSchema DocumentDetectedV1 = Load("DocumentDetected.v1.schema.json");
    public static readonly JsonSchema DocumentDownloadedV1 = Load("DocumentDownloaded.v1.schema.json");
    public static readonly JsonSchema DocumentExtractedV1 = Load("DocumentExtracted.v1.schema.json");
    public static readonly JsonSchema OcrCompletedV1 = Load("OcrCompleted.v1.schema.json");
    public static readonly JsonSchema DocumentChunkedV1 = Load("DocumentChunked.v1.schema.json");

    private static JsonSchema Load(string fileName) =>
        JsonSchema.FromFile(Path.Combine(SchemasDir, fileName));
}
