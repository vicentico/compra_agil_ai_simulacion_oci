namespace Ppip.DocumentIntelligence.Infrastructure.Persistence;

/// <summary>Config esperada: <c>Ppip:Mongo:ConnectionString</c> (compartida con los demás módulos) + <c>Ppip:Mongo:DocumentsDatabaseName</c>.</summary>
public sealed class MongoOptions
{
    public const string SectionName = "Ppip:Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Base lógica "documents" (docs/08-data/01-data-architecture.md) — propia de Document Intelligence, separada de "procurement".</summary>
    public string DocumentsDatabaseName { get; set; } = "documents";
}
