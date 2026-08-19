using MongoDB.Driver;
using Ppip.Knowledge.Domain;
using Ppip.Knowledge.Infrastructure.Persistence;
using Testcontainers.MongoDb;
using Xunit;

namespace Ppip.Knowledge.Infrastructure.Tests.Persistence;

/// <summary>Contra un MongoDB real — valida el round-trip de `ai_executions` (docs/08-data/01, UC-005 paso 8).</summary>
public sealed class MongoAIExecutionRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private IMongoDatabase _database = null!;
    private MongoAIExecutionRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        _database = client.GetDatabase("knowledge_ai_executions_test");
        _repository = new MongoAIExecutionRepository(new KnowledgeMongoDatabaseProvider(_database));
        await MongoAIExecutionRepository.EnsureIndexesAsync(_database);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task SaveAsync_PersistsAllFields()
    {
        var correlationId = Guid.CreateVersion7();
        var execution = AIExecution.Record("418-1191-COT26", "llama3.1:8b", "rag-answer-v1.0", 2314, 118, 2140, correlationId);

        await _repository.SaveAsync(execution);

        var collection = _database.GetCollection<MongoDB.Bson.BsonDocument>("ai_executions");
        var stored = await collection.Find(new MongoDB.Bson.BsonDocument()).FirstOrDefaultAsync();
        Assert.NotNull(stored);
        Assert.Equal("418-1191-COT26", stored["CompraAgilId"].AsString);
        Assert.Equal("llama3.1:8b", stored["Model"].AsString);
        Assert.Equal(2314, stored["TokensIn"].AsInt32);
    }

    [Fact]
    public async Task SaveAsync_AppendOnly_MultipleExecutionsAllPersist()
    {
        var correlationId = Guid.CreateVersion7();
        await _repository.SaveAsync(AIExecution.Record("418-1191-COT26", "n/a", "rag-answer-v1.0", 0, 0, 0, correlationId));
        await _repository.SaveAsync(AIExecution.Record("418-1191-COT26", "llama3.1:8b", "rag-answer-v1.0", 100, 20, 500, correlationId));

        var collection = _database.GetCollection<MongoDB.Bson.BsonDocument>("ai_executions");
        var count = await collection.CountDocumentsAsync(new MongoDB.Bson.BsonDocument());
        Assert.Equal(2, count);
    }
}
