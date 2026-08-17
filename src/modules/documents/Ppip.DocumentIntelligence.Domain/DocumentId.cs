using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>Identidad del agregado <see cref="Document"/> (docs/03-domain/02-domain-model.md) — UUID v7, propia del sistema (no viene de ChileCompra).</summary>
public sealed class DocumentId : ValueObject
{
    public Guid Value { get; }

    private DocumentId(Guid value) => Value = value;

    public static DocumentId New() => new(Guid.CreateVersion7());

    public static DocumentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("El id del documento no puede ser vacío.", nameof(value));
        }

        return new DocumentId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
