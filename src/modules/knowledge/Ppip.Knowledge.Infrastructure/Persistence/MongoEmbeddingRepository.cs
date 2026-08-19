using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.Knowledge.Domain;
using Ppip.Knowledge.Domain.Ports;

namespace Ppip.Knowledge.Infrastructure.Persistence;

/// <summary>Adaptador Mongo de <see cref="IEmbeddingRepository"/> — colección `embeddings` (docs/08-data/01: referencias, nunca el vector en sí).</summary>
public sealed class MongoEmbeddingRepository : IEmbeddingRepository
{
    private readonly IMongoCollection<EmbeddingRecord> _collection;

    public MongoEmbeddingRepository(KnowledgeMongoDatabaseProvider database) =>
        _collection = database.Database.GetCollection<EmbeddingRecord>("embeddings");

    public async Task SaveAsync(Embedding embedding, CancellationToken cancellationToken = default)
    {
        var record = new EmbeddingRecord
        {
            Id = embedding.Id,
            ChunkId = embedding.ChunkId,
            ModelVersion = embedding.ModelVersion,
            Dimension = embedding.Dimension,
            VectorRef = embedding.VectorRef,
            CreatedAt = embedding.CreatedAt,
        };

        await _collection.ReplaceOneAsync(e => e.Id == embedding.Id, record, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<EmbeddingRecord>("embeddings");
        var keys = Builders<EmbeddingRecord>.IndexKeys.Ascending(e => e.ChunkId);
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<EmbeddingRecord>(keys, new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
    }

    private sealed class EmbeddingRecord
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid ChunkId { get; set; }

        public string ModelVersion { get; set; } = string.Empty;

        public int Dimension { get; set; }

        public string VectorRef { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
    }
}
