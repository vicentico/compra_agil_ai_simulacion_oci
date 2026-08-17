using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>Organismo comprador — agregado pequeño, identidad por código oficial.</summary>
public sealed class Institution : AggregateRoot<string>
{
    public string Nombre { get; private set; }

    private Institution(string codigoOficial, string nombre) : base(codigoOficial) => Nombre = nombre;

    public static Institution Create(string codigoOficial, string nombre)
    {
        if (string.IsNullOrWhiteSpace(codigoOficial))
        {
            throw new ArgumentException("El código oficial de la institución es obligatorio.", nameof(codigoOficial));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la institución es obligatorio.", nameof(nombre));
        }

        return new Institution(codigoOficial.Trim(), nombre.Trim());
    }

    public void Renombrar(string nuevoNombre)
    {
        if (string.IsNullOrWhiteSpace(nuevoNombre))
        {
            throw new ArgumentException("El nombre de la institución es obligatorio.", nameof(nuevoNombre));
        }

        Nombre = nuevoNombre.Trim();
    }
}
