using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>Monto + moneda (docs/03-domain/02-domain-model.md).</summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money From(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "El monto no puede ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("La moneda es obligatoria (p.ej. \"CLP\").", nameof(currency));
        }

        return new Money(amount, currency.Trim().ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
