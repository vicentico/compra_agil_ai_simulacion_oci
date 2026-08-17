namespace Ppip.Procurement.Infrastructure.Persistence;

/// <summary>Config esperada: <c>Ppip:Mongo:ConnectionString</c> (ya usada por los health checks desde FASE 1) + <c>Ppip:Mongo:DatabaseName</c>.</summary>
public sealed class MongoOptions
{
    public const string SectionName = "Ppip:Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Base lógica "procurement" (docs/08-data/01-data-architecture.md) — instancia compartida en el POC, separable por ADR-012 si un contexto se extrae.</summary>
    public string DatabaseName { get; set; } = "procurement";
}
