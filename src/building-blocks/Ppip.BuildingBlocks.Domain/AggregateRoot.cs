namespace Ppip.BuildingBlocks.Domain;

/// <summary>
/// Raíz de agregado: única entidad de un agregado que el exterior puede
/// referenciar directamente, y única que levanta <see cref="IDomainEvent"/>.
/// Los eventos se acumulan en memoria hasta que la capa de aplicación los
/// extrae (<see cref="PullDomainEvents"/>) tras persistir el agregado.
/// </summary>
public abstract class AggregateRoot<TId>(TId id) : Entity<TId>(id)
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}
