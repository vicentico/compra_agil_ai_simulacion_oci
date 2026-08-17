namespace Ppip.Procurement.Infrastructure.Messaging;

/// <summary>Config esperada: <c>Ppip:RabbitMq:*</c> (Host/Username/Password ya usados por el health check desde FASE 1).</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "Ppip:RabbitMq";

    public string Host { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Exchange topic de docs/07-events/00-event-conventions.md.</summary>
    public string Exchange { get; set; } = "ppip.events";
}
