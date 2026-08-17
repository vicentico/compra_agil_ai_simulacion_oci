namespace Ppip.Procurement.Domain;

/// <summary>
/// Estados de una Compra Ágil (docs/03-domain/02-domain-model.md). Las
/// transiciones válidas las decide <see cref="EstadoCompraTransitions"/>, no
/// el enum en sí.
/// </summary>
public enum EstadoCompra
{
    Publicada,
    Cerrada,
    Adjudicada,
    Desierta,
}

/// <summary>
/// Invariante de transición de <see cref="CompraAgil"/>: publicada → cerrada
/// → adjudicada/desierta. Adjudicada y desierta son estados finales.
/// </summary>
public static class EstadoCompraTransitions
{
    private static readonly Dictionary<EstadoCompra, EstadoCompra[]> Allowed = new()
    {
        [EstadoCompra.Publicada] = [EstadoCompra.Cerrada],
        [EstadoCompra.Cerrada] = [EstadoCompra.Adjudicada, EstadoCompra.Desierta],
        [EstadoCompra.Adjudicada] = [],
        [EstadoCompra.Desierta] = [],
    };

    public static bool IsValid(EstadoCompra from, EstadoCompra to) => Allowed[from].Contains(to);
}
