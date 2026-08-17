using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>Ventana de vigencia (publicación/cierre) de una Compra Ágil.</summary>
public sealed class DateRange : ValueObject
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    private DateRange(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    public static DateRange From(DateTimeOffset publicacion, DateTimeOffset cierre)
    {
        if (cierre < publicacion)
        {
            throw new ArgumentException("La fecha de cierre no puede ser anterior a la de publicación.");
        }

        return new DateRange(publicacion, cierre);
    }

    public bool IsOpenAt(DateTimeOffset moment) => moment >= Start && moment <= End;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
