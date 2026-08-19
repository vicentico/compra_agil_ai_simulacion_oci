using MongoDB.Driver;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Infrastructure.Persistence;
using Testcontainers.MongoDb;
using Xunit;

namespace Ppip.DocumentIntelligence.Infrastructure.Tests.Persistence;

/// <summary>Contra un MongoDB real — valida el round-trip de `document_chunks` (docs/08-data/01).</summary>
public sealed class MongoDocumentChunkRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private MongoDocumentChunkRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        var database = client.GetDatabase("documents_chunks_test");
        _repository = new MongoDocumentChunkRepository(database);
        await MongoDocumentChunkRepository.EnsureIndexesAsync(database);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task SaveManyThenFindByVersion_RoundTripsInPageOrder()
    {
        var documentId = DocumentId.New();
        var versionId = Guid.CreateVersion7();
        var chunks = new[]
        {
            DocumentChunk.Create(documentId, versionId, "418-1191-COT26", 2, "2. Requisitos", null, ChunkType.Requirement, "El oferente deberá cumplir con...", 6),
            DocumentChunk.Create(documentId, versionId, "418-1191-COT26", 1, "1. Objeto", null, ChunkType.Title, "1. Objeto", 2),
        };

        await _repository.SaveManyAsync(chunks);
        var found = await _repository.FindByVersionAsync(versionId);

        Assert.Equal(2, found.Count);
        Assert.Equal(1, found[0].Page);
        Assert.Equal(2, found[1].Page);
        Assert.Equal(ChunkType.Requirement, found[1].ChunkType);
    }

    [Fact]
    public async Task FindByVersion_UnknownVersion_ReturnsEmpty()
    {
        var found = await _repository.FindByVersionAsync(Guid.CreateVersion7());

        Assert.Empty(found);
    }

    [Fact]
    public async Task SaveManyAsync_EmptyList_DoesNothing()
    {
        await _repository.SaveManyAsync([]);
        var found = await _repository.FindByVersionAsync(Guid.CreateVersion7());

        Assert.Empty(found);
    }
}
