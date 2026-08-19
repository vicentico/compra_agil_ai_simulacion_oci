using System.Text.Json;
using Ppip.BuildingBlocks.Messaging;
using Ppip.Knowledge.Application.Events;

namespace Ppip.Knowledge.Application;

/// <summary>Construye el <see cref="EventEnvelope{T}"/> de integración y lo apila en el outbox — mismo patrón que <c>Ppip.DocumentIntelligence.Application.DocumentEventPublisher</c> (FASE 7-8).</summary>
public sealed class KnowledgeEventPublisher(IOutboxStore outbox)
{
    private const string Context = "knowledge";

    public Task PublishEmbeddingCreatedAsync(
        string documentId,
        string versionId,
        string modelVersion,
        int indexedCount,
        bool isLastOfCompra,
        string correlationId,
        string producer,
        CancellationToken cancellationToken = default)
    {
        var payload = new EmbeddingCreatedPayload(documentId, versionId, modelVersion, indexedCount, isLastOfCompra);
        return AppendAsync("EmbeddingCreated", 1, payload, correlationId, producer, cancellationToken);
    }

    private async Task AppendAsync<TPayload>(string eventType, int version, TPayload payload, string correlationId, string producer, CancellationToken cancellationToken)
    {
        var envelope = EventEnvelope<TPayload>.Create(eventType, version, correlationId, producer, payload);
        var json = JsonSerializer.Serialize(envelope);
        var message = new OutboxMessage(envelope.EventId, envelope.EventType, envelope.RoutingKey(Context), json, envelope.Timestamp);
        await outbox.AppendAsync(message, cancellationToken);
    }
}
