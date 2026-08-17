using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.BuildingBlocks.Messaging;

namespace Ppip.DocumentIntelligence.Infrastructure.Persistence;

/// <summary>
/// Adaptador Mongo de <see cref="IOutboxStore"/> propio de Document
/// Intelligence (colección <c>outbox_messages</c> en la base <c>documents</c>)
/// — no se comparte con el outbox de Procurement: cada bounded context es
/// dueño de sus propios datos (docs/08-data/01), incluido su outbox.
/// </summary>
public sealed class MongoOutboxStore : IOutboxStore
{
    private readonly IMongoCollection<OutboxMessageRecord> _collection;

    public MongoOutboxStore(IMongoDatabase database) =>
        _collection = database.GetCollection<OutboxMessageRecord>("outbox_messages");

    public Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var record = new OutboxMessageRecord
        {
            Id = message.Id,
            EventType = message.EventType,
            RoutingKey = message.RoutingKey,
            PayloadJson = message.PayloadJson,
            OccurredAt = message.OccurredAt,
            PublishedAt = message.PublishedAt,
        };

        return _collection.InsertOneAsync(record, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var records = await _collection
            .Find(d => d.PublishedAt == null)
            .SortBy(d => d.OccurredAt)
            .Limit(maxCount)
            .ToListAsync(cancellationToken);

        return [.. records.Select(ToDomain)];
    }

    public async Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        var update = Builders<OutboxMessageRecord>.Update.Set(d => d.PublishedAt, publishedAt);
        await _collection.UpdateOneAsync(d => d.Id == messageId, update, cancellationToken: cancellationToken);
    }

    private static OutboxMessage ToDomain(OutboxMessageRecord record)
    {
        var message = new OutboxMessage(record.Id, record.EventType, record.RoutingKey, record.PayloadJson, record.OccurredAt);
        if (record.PublishedAt is { } publishedAt)
        {
            message.MarkPublished(publishedAt);
        }

        return message;
    }

    private sealed class OutboxMessageRecord
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string RoutingKey { get; set; } = string.Empty;

        public string PayloadJson { get; set; } = string.Empty;

        public DateTimeOffset OccurredAt { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }
    }
}
