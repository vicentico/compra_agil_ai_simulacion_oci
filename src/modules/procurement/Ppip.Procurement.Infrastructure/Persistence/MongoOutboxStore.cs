using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.BuildingBlocks.Messaging;

namespace Ppip.Procurement.Infrastructure.Persistence;

/// <summary>
/// Adaptador Mongo de <see cref="IOutboxStore"/> (ADR-003): colección
/// <c>outbox_messages</c>, índice por <c>PublishedAt</c> para que el
/// dispatcher (<c>Ppip.Procurement.Infrastructure.Messaging.OutboxDispatcher</c>)
/// encuentre lo pendiente rápido. Primer adaptador real desde que el puerto
/// se definió en FASE 4.
/// </summary>
public sealed class MongoOutboxStore : IOutboxStore
{
    private readonly IMongoCollection<OutboxMessageDocument> _collection;

    public MongoOutboxStore(IMongoDatabase database) =>
        _collection = database.GetCollection<OutboxMessageDocument>("outbox_messages");

    public Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var document = new OutboxMessageDocument
        {
            Id = message.Id,
            EventType = message.EventType,
            RoutingKey = message.RoutingKey,
            PayloadJson = message.PayloadJson,
            OccurredAt = message.OccurredAt,
            PublishedAt = message.PublishedAt,
        };

        return _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var documents = await _collection
            .Find(d => d.PublishedAt == null)
            .SortBy(d => d.OccurredAt)
            .Limit(maxCount)
            .ToListAsync(cancellationToken);

        return [.. documents.Select(ToDomain)];
    }

    public async Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        var update = Builders<OutboxMessageDocument>.Update.Set(d => d.PublishedAt, publishedAt);
        await _collection.UpdateOneAsync(d => d.Id == messageId, update, cancellationToken: cancellationToken);
    }

    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<OutboxMessageDocument>("outbox_messages");
        var keys = Builders<OutboxMessageDocument>.IndexKeys.Ascending(d => d.PublishedAt);
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<OutboxMessageDocument>(keys), cancellationToken: cancellationToken);
    }

    private static OutboxMessage ToDomain(OutboxMessageDocument document)
    {
        var message = new OutboxMessage(document.Id, document.EventType, document.RoutingKey, document.PayloadJson, document.OccurredAt);
        if (document.PublishedAt is { } publishedAt)
        {
            message.MarkPublished(publishedAt);
        }

        return message;
    }

    private sealed class OutboxMessageDocument
    {
        // MongoDB.Driver 3.x exige representación explícita para Guid — sin
        // esto, serializar/filtrar por un Guid lanza en runtime
        // ("GuidRepresentation is Unspecified"). No lo detectó ningún test de
        // FASE 6 porque el único repo probado contra Mongo real
        // (MongoCompraAgilRepositoryTests) usa un id string, no Guid.
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
