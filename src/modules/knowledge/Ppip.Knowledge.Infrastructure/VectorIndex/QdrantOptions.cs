namespace Ppip.Knowledge.Infrastructure.VectorIndex;

/// <summary>Config esperada: <c>Ppip:Qdrant:*</c> (ya usado por health checks desde FASE 1/2).</summary>
public sealed class QdrantOptions
{
    public const string SectionName = "Ppip:Qdrant";

    public string Endpoint { get; set; } = "http://localhost:6333";

    public string? ApiKey { get; set; }

    /// <summary>docs/10-rag/01: cambiar de modelo de embeddings implica colección nueva — el nombre versiona el esquema.</summary>
    public string CollectionName { get; set; } = "chunks_v1";
}
