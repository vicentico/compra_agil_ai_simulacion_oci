namespace Ppip.Procurement.Domain.Ports;

/// <summary>
/// Puerto del agregado <see cref="CompraAgil"/> (NFR-013): la aplicación
/// depende de esto, nunca de MongoDB directamente. Adaptador real
/// (colección <c>compras_agiles</c>, unique index por código) en
/// <c>Ppip.Procurement.Infrastructure</c>, FASE 6.
/// </summary>
public interface ICompraAgilRepository
{
    Task<CompraAgil?> FindAsync(CompraAgilId id, CancellationToken cancellationToken = default);

    Task SaveAsync(CompraAgil compra, CancellationToken cancellationToken = default);
}
