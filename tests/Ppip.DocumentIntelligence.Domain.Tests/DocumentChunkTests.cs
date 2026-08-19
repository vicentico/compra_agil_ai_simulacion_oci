using Xunit;

namespace Ppip.DocumentIntelligence.Domain.Tests;

public class DocumentChunkTests
{
    [Fact]
    public void Create_Valid_ComputesHash()
    {
        var documentId = DocumentId.New();
        var versionId = Guid.CreateVersion7();

        var chunk = DocumentChunk.Create(documentId, versionId, "418-1191-COT26", 1, "1. Objeto", null, ChunkType.Paragraph, "El objeto de esta compra es...", tokenCount: 6);

        Assert.Equal(64, chunk.Hash.Length);
        Assert.Equal("1. Objeto", chunk.Section);
        Assert.Equal(ChunkType.Paragraph, chunk.ChunkType);
    }

    [Fact]
    public void Create_SameText_ProducesSameHash()
    {
        var documentId = DocumentId.New();
        var versionId = Guid.CreateVersion7();

        var a = DocumentChunk.Create(documentId, versionId, "418-1191-COT26", 1, null, null, ChunkType.Paragraph, "mismo texto", 2);
        var b = DocumentChunk.Create(documentId, versionId, "418-1191-COT26", 2, null, null, ChunkType.Paragraph, "mismo texto", 2);

        Assert.Equal(a.Hash, b.Hash);
    }

    [Fact]
    public void Create_EmptyText_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            DocumentChunk.Create(DocumentId.New(), Guid.CreateVersion7(), "418-1191-COT26", 1, null, null, ChunkType.Paragraph, "   ", 1));
    }

    [Fact]
    public void Create_ZeroTokenCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DocumentChunk.Create(DocumentId.New(), Guid.CreateVersion7(), "418-1191-COT26", 1, null, null, ChunkType.Paragraph, "texto", 0));
    }
}
