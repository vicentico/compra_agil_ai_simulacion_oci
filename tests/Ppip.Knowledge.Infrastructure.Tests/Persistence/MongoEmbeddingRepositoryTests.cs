using MongoDB.Driver;
using Ppip.Knowledge.Domain;
using Ppip.Knowledge.Infrastructure.Persistence;
using Testcontainers.MongoDb;
using Xunit;

namespace Ppip.Knowledge.Infrastructure.Tests.Persistence;

/// <summary>Contra un MongoDB real — valida el round-trip de `embeddings` (docs/08-data/01: referencias, no vectores).</summary>
public sealed class MongoEmbeddingRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private IMongoDatabase _database = null!;
    private MongoEmbeddingRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        _database = client.GetDatabase("knowledge_embeddings_test");
        _repository = new MongoEmbeddingRepository(new KnowledgeMongoDatabaseProvider(_database));
        await MongoEmbeddingRepository.EnsureIndexesAsync(_database);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task SaveAsync_PersistsReference_NotTheVector()
    {
        var embedding = Embedding.Create(Guid.CreateVersion7(), "nomic-embed-text", 768, Guid.CreateVersion7().ToString());

        await _repository.SaveAsync(embedding);

        var collection = _database.GetCollection<MongoDB.Bson.BsonDocument>("embeddings");
        var stored = await collection.Find(new MongoDB.Bson.BsonDocument()).FirstOrDefaultAsync();
        Assert.NotNull(stored);
        Assert.False(stored.Contains("values"));
        Assert.False(stored.Contains("vector"));
        Assert.Equal(embedding.VectorRef, stored["VectorRef"].AsString);
    }

    [Fact]
    public async Task SaveAsync_SameId_Upserts()
    {
        var chunkId = Guid.CreateVersion7();
        var embedding = Embedding.Create(chunkId, "nomic-embed-text", 768, "ref-1");
        await _repository.SaveAsync(embedding);

        var reindexed = Embedding.Rehydrate(embedding.Id, chunkId, "nomic-embed-text", 768, "ref-2", embedding.CreatedAt);
        await _repository.SaveAsync(reindexed);

        var collection = _database.GetCollection<MongoDB.Bson.BsonDocument>("embeddings");
        var count = await collection.CountDocumentsAsync(new MongoDB.Bson.BsonDocument());
        Assert.Equal(1, count);
    }
}
