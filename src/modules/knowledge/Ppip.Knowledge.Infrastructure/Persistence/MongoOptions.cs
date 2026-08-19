namespace Ppip.Knowledge.Infrastructure.Persistence;

public sealed class MongoOptions
{
    public const string SectionName = "Ppip:Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Base lógica "knowledge" (docs/08-data/01-data-architecture.md): ai_analyses, ai_executions, embeddings(refs) — propia de Knowledge/RAG.</summary>
    public string KnowledgeDatabaseName { get; set; } = "knowledge";
}
