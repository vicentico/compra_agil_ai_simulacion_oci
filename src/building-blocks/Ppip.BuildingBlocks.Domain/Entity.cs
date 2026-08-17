namespace Ppip.BuildingBlocks.Domain;

/// <summary>
/// Entidad con identidad estable: la igualdad se basa en <see cref="Id"/> y el
/// tipo concreto, nunca en los demás campos (a diferencia de <see cref="ValueObject"/>).
/// </summary>
public abstract class Entity<TId>(TId id) : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; } = id;

    public bool Equals(Entity<TId>? other) =>
        other is not null && (ReferenceEquals(this, other) || (GetType() == other.GetType() && Id.Equals(other.Id)));

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
