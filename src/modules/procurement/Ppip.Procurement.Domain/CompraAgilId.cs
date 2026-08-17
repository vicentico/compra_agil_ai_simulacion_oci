using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>Código oficial de la Compra Ágil en ChileCompra — identidad del agregado.</summary>
public sealed class CompraAgilId : ValueObject
{
    public string Value { get; }

    private CompraAgilId(string value) => Value = value;

    public static CompraAgilId From(string codigoChileCompra)
    {
        if (string.IsNullOrWhiteSpace(codigoChileCompra))
        {
            throw new ArgumentException("El código de Compra Ágil no puede estar vacío.", nameof(codigoChileCompra));
        }

        return new CompraAgilId(codigoChileCompra.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
