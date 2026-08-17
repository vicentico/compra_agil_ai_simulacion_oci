using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Domain.Ports;

namespace Ppip.Procurement.Infrastructure.Persistence;

/// <summary>Colección <c>raw_payloads</c> (docs/08-data): inmutable e imborrable — solo inserta, nunca actualiza ni borra.</summary>
public sealed class MongoRawPayloadRepository : IRawPayloadRepository
{
    private readonly IMongoCollection<RawPayloadDocument> _collection;

    public MongoRawPayloadRepository(IMongoDatabase database) =>
        _collection = database.GetCollection<RawPayloadDocument>("raw_payloads");

    public Task SaveAsync(Guid rawPayloadId, string codigo, RawCompraAgilPayload payload, CancellationToken cancellationToken = default)
    {
        var document = new RawPayloadDocument
        {
            Id = rawPayloadId,
            Codigo = codigo,
            Payload = payload.Payload,
            SourceUrl = payload.SourceUrl,
            RetrievedAt = payload.RetrievedAt,
            HttpStatus = payload.HttpStatus,
            ResponseHash = payload.ResponseHash,
            ApiVersion = payload.ApiVersion,
            CorrelationId = payload.CorrelationId,
        };

        return _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    private sealed class RawPayloadDocument
    {
        // MongoDB.Driver 3.x exige representación explícita para Guid (ver comentario equivalente en MongoOutboxStore).
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public string SourceUrl { get; set; } = string.Empty;

        public DateTimeOffset RetrievedAt { get; set; }

        public int HttpStatus { get; set; }

        public string ResponseHash { get; set; } = string.Empty;

        public string ApiVersion { get; set; } = string.Empty;

        public string CorrelationId { get; set; } = string.Empty;
    }
}
