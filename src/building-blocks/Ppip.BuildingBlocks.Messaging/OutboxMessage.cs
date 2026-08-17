namespace Ppip.BuildingBlocks.Messaging;

/// <summary>
/// Registro de outbox (ADR-003): se persiste en la misma transacción que el
/// cambio de negocio que lo originó, y un dispatcher separado lo publica a
/// RabbitMQ y lo marca publicado — así ningún evento confirmado se pierde
/// aunque el broker esté caído (F10, docs/14-reliability/01).
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; }
    public string EventType { get; }
    public string RoutingKey { get; }
    public string PayloadJson { get; }
    public DateTimeOffset OccurredAt { get; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public OutboxMessage(Guid id, string eventType, string routingKey, string payloadJson, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("El eventType es obligatorio.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(routingKey))
        {
            throw new ArgumentException("El routingKey es obligatorio.", nameof(routingKey));
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ArgumentException("El payloadJson es obligatorio.", nameof(payloadJson));
        }

        Id = id;
        EventType = eventType;
        RoutingKey = routingKey;
        PayloadJson = payloadJson;
        OccurredAt = occurredAt;
    }

    public bool IsPublished => PublishedAt is not null;

    public void MarkPublished(DateTimeOffset publishedAt)
    {
        if (IsPublished)
        {
            throw new InvalidOperationException($"El mensaje {Id} ya fue publicado en {PublishedAt:O}.");
        }

        PublishedAt = publishedAt;
    }
}
