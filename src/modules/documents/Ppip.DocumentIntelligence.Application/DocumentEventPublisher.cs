using System.Text.Json;
using Ppip.BuildingBlocks.Messaging;
using Ppip.DocumentIntelligence.Application.Events;
using Ppip.DocumentIntelligence.Domain;

namespace Ppip.DocumentIntelligence.Application;

/// <summary>Construye el <see cref="EventEnvelope{T}"/> de integración y lo apila en el outbox — mismo patrón que <c>Ppip.Procurement.Application.ProcurementEventPublisher</c> (FASE 6).</summary>
public sealed class DocumentEventPublisher(IOutboxStore outbox)
{
    private const string Context = "document";

    public Task PublishDetectedAsync(Document document, string correlationId, string producer, CancellationToken cancellationToken = default)
    {
        var payload = new DocumentDetectedPayload(document.Id.ToString(), document.CompraAgilId, document.SourceUrl, document.DeclaredName);
        return AppendAsync("DocumentDetected", 1, payload, correlationId, producer, cancellationToken);
    }

    public Task PublishDownloadedAsync(Document document, string correlationId, string producer, CancellationToken cancellationToken = default)
    {
        var version = document.CurrentVersion ?? throw new InvalidOperationException("El documento no tiene versión descargada.");
        var payload = new DocumentDownloadedPayload(
            DocumentId: document.Id.ToString(),
            CompraAgilId: document.CompraAgilId,
            VersionId: version.Id.ToString(),
            Sha256: version.Sha256Hash.Value,
            StorageRef: new StorageRefPayload(version.StorageRef.Bucket, version.StorageRef.Key),
            SizeBytes: version.SizeBytes);

        return AppendAsync("DocumentDownloaded", 1, payload, correlationId, producer, cancellationToken);
    }

    private async Task AppendAsync<TPayload>(string eventType, int version, TPayload payload, string correlationId, string producer, CancellationToken cancellationToken)
    {
        var envelope = EventEnvelope<TPayload>.Create(eventType, version, correlationId, producer, payload);
        var json = JsonSerializer.Serialize(envelope);
        var message = new OutboxMessage(envelope.EventId, envelope.EventType, envelope.RoutingKey(Context), json, envelope.Timestamp);
        await outbox.AppendAsync(message, cancellationToken);
    }
}
