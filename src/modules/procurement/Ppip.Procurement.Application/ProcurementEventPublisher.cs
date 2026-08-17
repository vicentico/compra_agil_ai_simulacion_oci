using System.Text.Json;
using Ppip.BuildingBlocks.Messaging;
using Ppip.Procurement.Application.Events;
using Ppip.Procurement.Domain;

namespace Ppip.Procurement.Application;

/// <summary>
/// Construye el <see cref="EventEnvelope{T}"/> de integración (contrato de
/// docs/07-events/schemas/) a partir del agregado ya guardado y lo apila en
/// el outbox (ADR-003) — nunca publica directo a RabbitMQ (el dispatcher del
/// outbox, en Infrastructure, lo hace de forma separada y reintentable).
/// </summary>
public sealed class ProcurementEventPublisher(IOutboxStore outbox)
{
    private const string Context = "procurement";

    public Task PublishDetectedAsync(CompraAgil compra, Guid rawPayloadId, string correlationId, string producer, CancellationToken cancellationToken = default)
    {
        var payload = new CompraAgilDetectedPayload(
            CompraAgilId: compra.Id.Value,
            Codigo: compra.Id.Value,
            Nombre: compra.Titulo,
            OrganismoCodigo: compra.Institution.Id,
            FechaCierre: compra.Vigencia.End,
            MontoDisponible: new MoneyPayload(compra.MontoEstimado.Amount, compra.MontoEstimado.Currency),
            RawPayloadId: rawPayloadId.ToString(),
            DocumentRefs: []);

        return AppendAsync("CompraAgilDetected", 1, payload, correlationId, producer, cancellationToken);
    }

    public Task PublishUpdatedAsync(CompraAgil compra, IReadOnlyList<string> changedFields, Guid rawPayloadId, string correlationId, string producer, CancellationToken cancellationToken = default)
    {
        var payload = new CompraAgilUpdatedPayload(
            CompraAgilId: compra.Id.Value,
            Version: compra.Version,
            ChangedFields: changedFields,
            RawPayloadId: rawPayloadId.ToString());

        return AppendAsync("CompraAgilUpdated", 1, payload, correlationId, producer, cancellationToken);
    }

    private async Task AppendAsync<TPayload>(string eventType, int version, TPayload payload, string correlationId, string producer, CancellationToken cancellationToken)
    {
        var envelope = EventEnvelope<TPayload>.Create(eventType, version, correlationId, producer, payload);
        var json = JsonSerializer.Serialize(envelope);
        var message = new OutboxMessage(envelope.EventId, envelope.EventType, envelope.RoutingKey(Context), json, envelope.Timestamp);
        await outbox.AppendAsync(message, cancellationToken);
    }
}
