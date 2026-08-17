using System.Text;

namespace Ppip.BuildingBlocks.Messaging;

/// <summary>
/// Envelope estándar de todo evento publicado en <c>ppip.events</c>
/// (docs/07-events/00-event-conventions.md). <typeparamref name="TPayload"/>
/// es el contrato específico del evento (p.ej. el payload de
/// CompraAgilDetected.v1) — mínimo, solo ids + hechos del cambio (regla 6).
/// </summary>
public sealed record EventEnvelope<TPayload>(
    Guid EventId,
    string EventType,
    int Version,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string? CausationId,
    string Producer,
    bool IsDemoData,
    TPayload Payload)
{
    /// <summary>
    /// Construye un envelope nuevo: <c>EventId</c> como UUID v7 (ordenable
    /// por tiempo) y <c>Timestamp</c> en el momento de la llamada — ambos
    /// nunca los decide el caller, para que todo evento sea comparable.
    /// </summary>
    public static EventEnvelope<TPayload> Create(
        string eventType,
        int version,
        string correlationId,
        string producer,
        TPayload payload,
        string? causationId = null,
        bool isDemoData = false)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("El eventType es obligatorio.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("El correlationId es obligatorio (docs/06-api/00-api-conventions.md).", nameof(correlationId));
        }

        if (string.IsNullOrWhiteSpace(producer))
        {
            throw new ArgumentException("El producer es obligatorio (p.ej. \"sync-worker@1.4.0\").", nameof(producer));
        }

        return new EventEnvelope<TPayload>(
            EventId: Guid.CreateVersion7(),
            EventType: eventType,
            Version: version,
            Timestamp: DateTimeOffset.UtcNow,
            CorrelationId: correlationId,
            CausationId: causationId,
            Producer: producer,
            IsDemoData: isDemoData,
            Payload: payload);
    }

    /// <summary>
    /// Routing key <c>contexto.nombre-evento.vN</c> (regla 1, versionado) —
    /// p.ej. <c>procurement.compra-agil-detected.v1</c> para el eventType
    /// <c>CompraAgilDetected</c> (docs/07-events/01-example-compra-agil-detected.md).
    /// </summary>
    public string RoutingKey(string context) => $"{context}.{ToKebabCase(EventType)}.v{Version}";

    private static string ToKebabCase(string pascalCase)
    {
        var builder = new StringBuilder(pascalCase.Length + 4);
        for (var i = 0; i < pascalCase.Length; i++)
        {
            var c = pascalCase[i];
            if (char.IsUpper(c) && i > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
