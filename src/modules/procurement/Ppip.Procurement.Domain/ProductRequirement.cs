using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>Ítem solicitado dentro de una Compra Ágil: producto, cantidad, unidad.</summary>
public sealed class ProductRequirement : Entity<Guid>
{
    public string ProductName { get; }
    public decimal Quantity { get; }
    public string Unit { get; }

    private ProductRequirement(Guid id, string productName, decimal quantity, string unit) : base(id)
    {
        ProductName = productName;
        Quantity = quantity;
        Unit = unit;
    }

    public static ProductRequirement Create(string productName, decimal quantity, string unit)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("El producto es obligatorio.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "La cantidad debe ser mayor a cero.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("La unidad es obligatoria.", nameof(unit));
        }

        return new ProductRequirement(Guid.CreateVersion7(), productName.Trim(), quantity, unit.Trim());
    }
}
