using System.Security.Cryptography;
using System.Text;
using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>
/// Fragmento semántico de una <see cref="DocumentVersion"/> (docs/03-domain/02:
/// "hija lógica de Document" — colección propia <c>document_chunks</c>, no
/// anidada en el agregado). Inmutable una vez creado; una nueva versión del
/// documento genera chunks nuevos, nunca edita los existentes.
/// </summary>
public sealed class DocumentChunk : Entity<Guid>
{
    public DocumentId DocumentId { get; }
    public Guid VersionId { get; }
    public string CompraAgilId { get; }
    public int Page { get; }
    public string? Section { get; }
    public string? SubSection { get; }
    public ChunkType ChunkType { get; }
    public string Text { get; }
    public string Hash { get; }
    public int TokenCount { get; }
    public Guid? EmbeddingId { get; private set; }

    private DocumentChunk(
        Guid id,
        DocumentId documentId,
        Guid versionId,
        string compraAgilId,
        int page,
        string? section,
        string? subSection,
        ChunkType chunkType,
        string text,
        string hash,
        int tokenCount)
        : base(id)
    {
        DocumentId = documentId;
        VersionId = versionId;
        CompraAgilId = compraAgilId;
        Page = page;
        Section = section;
        SubSection = subSection;
        ChunkType = chunkType;
        Text = text;
        Hash = hash;
        TokenCount = tokenCount;
    }

    public static DocumentChunk Create(
        DocumentId documentId,
        Guid versionId,
        string compraAgilId,
        int page,
        string? section,
        string? subSection,
        ChunkType chunkType,
        string text,
        int tokenCount)
    {
        if (string.IsNullOrWhiteSpace(compraAgilId))
        {
            throw new ArgumentException("El id de la Compra Ágil es obligatorio.", nameof(compraAgilId));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("El texto del chunk no puede estar vacío.", nameof(text));
        }

        if (tokenCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenCount), tokenCount, "El conteo de tokens debe ser mayor a cero.");
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        return new DocumentChunk(Guid.CreateVersion7(), documentId, versionId, compraAgilId, page, section, subSection, chunkType, text, hash, tokenCount);
    }

    /// <summary>Usado por los repositorios para reconstruir desde almacenamiento.</summary>
    public static DocumentChunk Rehydrate(
        Guid id,
        DocumentId documentId,
        Guid versionId,
        string compraAgilId,
        int page,
        string? section,
        string? subSection,
        ChunkType chunkType,
        string text,
        string hash,
        int tokenCount,
        Guid? embeddingId = null)
    {
        var chunk = new DocumentChunk(id, documentId, versionId, compraAgilId, page, section, subSection, chunkType, text, hash, tokenCount);
        chunk.EmbeddingId = embeddingId;
        return chunk;
    }

    /// <summary>
    /// FASE 9: vincula el chunk con el embedding generado (docs/09: etapa 10).
    /// Idempotente por diseño — reindexar el mismo chunk reemplaza la referencia,
    /// nunca falla (el pipeline de embedding puede reintentarse por batch parcial).
    /// </summary>
    public void MarkEmbedded(Guid embeddingId) => EmbeddingId = embeddingId;
}
