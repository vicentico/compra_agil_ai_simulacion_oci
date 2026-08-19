using Ppip.Knowledge.Domain;
using Xunit;

namespace Ppip.Knowledge.Domain.Tests;

public class EmbeddingTests
{
    [Fact]
    public void Create_Valid_SetsFields()
    {
        var chunkId = Guid.CreateVersion7();

        var embedding = Embedding.Create(chunkId, "nomic-embed-text", 768, chunkId.ToString());

        Assert.Equal(chunkId, embedding.ChunkId);
        Assert.Equal("nomic-embed-text", embedding.ModelVersion);
        Assert.Equal(768, embedding.Dimension);
        Assert.Equal(chunkId.ToString(), embedding.VectorRef);
        Assert.NotEqual(Guid.Empty, embedding.Id);
    }

    [Fact]
    public void Create_EmptyModelVersion_Throws()
    {
        Assert.Throws<ArgumentException>(() => Embedding.Create(Guid.CreateVersion7(), "   ", 768, "ref"));
    }

    [Fact]
    public void Create_ZeroDimension_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Embedding.Create(Guid.CreateVersion7(), "nomic-embed-text", 0, "ref"));
    }

    [Fact]
    public void Create_EmptyVectorRef_Throws()
    {
        Assert.Throws<ArgumentException>(() => Embedding.Create(Guid.CreateVersion7(), "nomic-embed-text", 768, "  "));
    }

    [Fact]
    public void Rehydrate_PreservesAllFields()
    {
        var id = Guid.CreateVersion7();
        var chunkId = Guid.CreateVersion7();
        var createdAt = DateTimeOffset.UtcNow;

        var embedding = Embedding.Rehydrate(id, chunkId, "nomic-embed-text", 768, "vec-ref", createdAt);

        Assert.Equal(id, embedding.Id);
        Assert.Equal(chunkId, embedding.ChunkId);
        Assert.Equal("nomic-embed-text", embedding.ModelVersion);
        Assert.Equal(768, embedding.Dimension);
        Assert.Equal("vec-ref", embedding.VectorRef);
        Assert.Equal(createdAt, embedding.CreatedAt);
    }
}
