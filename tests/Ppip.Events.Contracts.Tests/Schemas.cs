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

    private static JsonSchema Load(string fileName) =>
        JsonSchema.FromFile(Path.Combine(SchemasDir, fileName));
}
