namespace Ppip.Knowledge.Application;

/// <summary>
/// Config esperada: <c>Ppip:Knowledge:Indexing:*</c> (FASE 9). OQ-03 (docs/01-discovery/09-open-questions.md):
/// modelo de embeddings elegido — nomic-embed-text (Ollama local, 768 dimensiones).
/// Cambiar el modelo implica colección Qdrant nueva + re-embedding (docs/10-rag/01).
/// </summary>
public sealed class EmbeddingIndexingOptions
{
    public const string SectionName = "Ppip:Knowledge:Indexing";

    /// <summary>Valor libre del payload Qdrant `source` (docs/10-rag/01) — identifica el origen del chunk.</summary>
    public string Source { get; set; } = "chilecompra";

    public bool IsDemoData { get; set; }

    public string Producer { get; set; } = "document-worker@1.0.0";
}
