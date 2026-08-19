using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.Knowledge.Domain;
using Ppip.Knowledge.Domain.Ports;

namespace Ppip.Knowledge.Infrastructure.Persistence;

/// <summary>Adaptador Mongo de <see cref="IAIExecutionRepository"/> — colección `ai_executions` (docs/08-data/01, índices por correlationId y promptVersion+modelVersion).</summary>
public sealed class MongoAIExecutionRepository : IAIExecutionRepository
{
    private readonly IMongoCollection<AIExecutionRecord> _collection;

    public MongoAIExecutionRepository(KnowledgeMongoDatabaseProvider database) =>
        _collection = database.Database.GetCollection<AIExecutionRecord>("ai_executions");

    public async Task SaveAsync(AIExecution execution, CancellationToken cancellationToken = default)
    {
        var record = new AIExecutionRecord
        {
            Id = execution.Id,
            CompraAgilId = execution.CompraAgilId,
            Model = execution.Model,
            PromptVersion = execution.PromptVersion,
            TokensIn = execution.TokensIn,
            TokensOut = execution.TokensOut,
            DurationMs = execution.DurationMs,
            CorrelationId = execution.CorrelationId,
            ExecutedAt = execution.ExecutedAt,
        };

        await _collection.InsertOneAsync(record, cancellationToken: cancellationToken);
    }

    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<AIExecutionRecord>("ai_executions");
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<AIExecutionRecord>(Builders<AIExecutionRecord>.IndexKeys.Ascending(e => e.CorrelationId)),
            cancellationToken: cancellationToken);
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<AIExecutionRecord>(Builders<AIExecutionRecord>.IndexKeys.Ascending(e => e.PromptVersion).Ascending(e => e.Model)),
            cancellationToken: cancellationToken);
    }

    private sealed class AIExecutionRecord
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        public string CompraAgilId { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string PromptVersion { get; set; } = string.Empty;

        public int TokensIn { get; set; }

        public int TokensOut { get; set; }

        public long DurationMs { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid CorrelationId { get; set; }

        public DateTimeOffset ExecutedAt { get; set; }
    }
}
