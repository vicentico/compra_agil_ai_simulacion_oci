namespace Ppip.Knowledge.Infrastructure.Embeddings;

/// <summary>
/// Config esperada: <c>Ppip:Knowledge:Embeddings:*</c> (FASE 9). OQ-03 (docs/01-discovery/09-open-questions.md,
/// docs/10-rag/01-rag-specification.md): modelo elegido — nomic-embed-text
/// (Ollama local, 768 dimensiones). Cambiar de modelo implica colección
/// Qdrant nueva + re-embedding (la dimensión fija la colección).
/// </summary>
public sealed class EmbeddingProviderOptions
{
    public const string SectionName = "Ppip:Knowledge:Embeddings";

    /// <summary><c>Mock</c> (por defecto, sin dependencias) o <c>Ollama</c> (real, no validado contra un modelo real en esta sesión — mismo criterio que TesseractOcrService, FASE 8).</summary>
    public string Provider { get; set; } = "Mock";

    public string ModelVersion { get; set; } = "nomic-embed-text";

    public int Dimension { get; set; } = 768;
}
