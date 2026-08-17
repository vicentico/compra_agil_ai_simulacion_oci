using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Domain.Ports;

namespace Ppip.Procurement.Infrastructure.Persistence;

public sealed class MongoSyncCheckpointRepository : ISyncCheckpointRepository
{
    private readonly IMongoCollection<SyncCheckpointDocument> _collection;

    public MongoSyncCheckpointRepository(IMongoDatabase database) =>
        _collection = database.GetCollection<SyncCheckpointDocument>("sync_checkpoints");

    public async Task<SyncCheckpoint?> FindAsync(string source, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(d => d.Id == source).FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        var checkpoint = SyncCheckpoint.Initial(document.Id, document.WindowStart, document.WindowEnd);
        if (document.LastSuccessfulSync is { } lastSuccessfulSync)
        {
            checkpoint.Advance(lastSuccessfulSync, document.WindowStart, document.WindowEnd);
        }

        return checkpoint;
    }

    public async Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        var document = new SyncCheckpointDocument
        {
            Id = checkpoint.Id,
            LastSuccessfulSync = checkpoint.LastSuccessfulSync,
            WindowStart = checkpoint.WindowStart,
            WindowEnd = checkpoint.WindowEnd,
        };

        await _collection.ReplaceOneAsync(
            d => d.Id == document.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private sealed class SyncCheckpointDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        public DateTimeOffset? LastSuccessfulSync { get; set; }

        public DateTimeOffset WindowStart { get; set; }

        public DateTimeOffset WindowEnd { get; set; }
    }
}
