namespace Ppip.BuildingBlocks.Domain;

/// <summary>
/// Hecho de negocio ya ocurrido, levantado por un <see cref="AggregateRoot{TId}"/>.
/// La traducción a un evento de integración (envelope, routing key, outbox)
/// es responsabilidad de la capa de aplicación — el dominio no la conoce
/// (NFR-013).
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
