using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>Referencia por id a <see cref="Institution"/> (contextos no comparten tipos internos).</summary>
public sealed class InstitutionRef : ValueObject
{
    public string Id { get; }
    public string Name { get; }

    private InstitutionRef(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public static InstitutionRef From(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("El id de la institución es obligatorio.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre de la institución es obligatorio.", nameof(name));
        }

        return new InstitutionRef(id.Trim(), name.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
    }
}
