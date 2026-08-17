using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Domain.Ports;

namespace Ppip.Procurement.Infrastructure.Persistence;

/// <summary>
/// Bitácora append-only de ciclos de sync (docs/08-data). Nada dentro de
/// FASE 6 vuelve a leer una <c>SyncExecution</c> ya guardada — es auditoría,
/// no estado operacional — así que este repositorio solo escribe.
/// </summary>
public sealed class MongoSyncExecutionRepository : ISyncExecutionRepository
{
    private readonly IMongoCollection<SyncExecutionDocument> _collection;

    public MongoSyncExecutionRepository(IMongoDatabase database) =>
        _collection = database.GetCollection<SyncExecutionDocument>("sync_executions");

    public Task SaveAsync(SyncExecution execution, CancellationToken cancellationToken = default)
    {
        var document = new SyncExecutionDocument
        {
            Id = execution.Id,
            CorrelationId = execution.CorrelationId,
            StartedAt = execution.StartedAt,
            FinishedAt = execution.FinishedAt,
            Created = execution.Created,
            Updated = execution.Updated,
            Unchanged = execution.Unchanged,
            Errors = execution.Errors,
            Status = execution.Status.ToString(),
        };

        return _collection.ReplaceOneAsync(
            d => d.Id == document.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private sealed class SyncExecutionDocument
    {
        [BsonId]
        public Guid Id { get; set; }

        public string CorrelationId { get; set; } = string.Empty;

        public DateTimeOffset StartedAt { get; set; }

        public DateTimeOffset? FinishedAt { get; set; }

        public int Created { get; set; }

        public int Updated { get; set; }

        public int Unchanged { get; set; }

        public int Errors { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}
