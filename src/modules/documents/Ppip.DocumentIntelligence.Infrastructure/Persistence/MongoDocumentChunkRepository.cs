using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Persistence;

/// <summary>Adaptador Mongo de <see cref="IDocumentChunkRepository"/> — colección `document_chunks` (docs/08-data/01), append-only.</summary>
public sealed class MongoDocumentChunkRepository : IDocumentChunkRepository
{
    private readonly IMongoCollection<DocumentChunkRecord> _collection;

    public MongoDocumentChunkRepository(IMongoDatabase database) =>
        _collection = database.GetCollection<DocumentChunkRecord>("document_chunks");

    public async Task SaveManyAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        var records = chunks.Select(c => new DocumentChunkRecord
        {
            Id = c.Id,
            DocumentId = c.DocumentId.Value,
            VersionId = c.VersionId,
            CompraAgilId = c.CompraAgilId,
            Page = c.Page,
            Section = c.Section,
            SubSection = c.SubSection,
            ChunkType = c.ChunkType.ToString(),
            Text = c.Text,
            Hash = c.Hash,
            TokenCount = c.TokenCount,
            EmbeddingId = c.EmbeddingId,
        }).ToList();

        await _collection.InsertManyAsync(records, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> FindByVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var records = await _collection.Find(c => c.VersionId == versionId).SortBy(c => c.Page).ToListAsync(cancellationToken);
        return [.. records.Select(ToDomain)];
    }

    public async Task<IReadOnlyList<DocumentChunk>> FindByIdsAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken = default)
    {
        if (chunkIds.Count == 0)
        {
            return [];
        }

        var records = await _collection.Find(Builders<DocumentChunkRecord>.Filter.In(c => c.Id, chunkIds)).ToListAsync(cancellationToken);
        return [.. records.Select(ToDomain)];
    }

    public Task MarkEmbeddedAsync(Guid chunkId, Guid embeddingId, CancellationToken cancellationToken = default) =>
        _collection.UpdateOneAsync(
            c => c.Id == chunkId,
            Builders<DocumentChunkRecord>.Update.Set(c => c.EmbeddingId, embeddingId),
            cancellationToken: cancellationToken);

    private static DocumentChunk ToDomain(DocumentChunkRecord r) => DocumentChunk.Rehydrate(
        r.Id,
        DocumentId.From(r.DocumentId),
        r.VersionId,
        r.CompraAgilId,
        r.Page,
        r.Section,
        r.SubSection,
        Enum.Parse<ChunkType>(r.ChunkType),
        r.Text,
        r.Hash,
        r.TokenCount,
        r.EmbeddingId);

    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<DocumentChunkRecord>("document_chunks");
        var keys = Builders<DocumentChunkRecord>.IndexKeys.Ascending(c => c.VersionId).Ascending(c => c.Page);
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<DocumentChunkRecord>(keys), cancellationToken: cancellationToken);
    }

    private sealed class DocumentChunkRecord
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid DocumentId { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid VersionId { get; set; }

        public string CompraAgilId { get; set; } = string.Empty;

        public int Page { get; set; }

        public string? Section { get; set; }

        public string? SubSection { get; set; }

        public string ChunkType { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public int TokenCount { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid? EmbeddingId { get; set; }
    }
}
