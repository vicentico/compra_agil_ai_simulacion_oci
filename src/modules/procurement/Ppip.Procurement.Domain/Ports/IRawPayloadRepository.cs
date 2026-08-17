namespace Ppip.Procurement.Domain.Ports;

/// <summary>
/// Puerto de <see cref="RawCompraAgilPayload"/> (docs/08-data: colección
/// <c>raw_payloads</c>, inmutable e imborrable — fuente de todo reproceso,
/// NFR-017). El llamador decide el id (UUID v7) para poder referenciarlo
/// desde el evento de integración (<c>rawPayloadId</c>) antes de guardarlo.
/// </summary>
public interface IRawPayloadRepository
{
    Task SaveAsync(Guid rawPayloadId, string codigo, RawCompraAgilPayload payload, CancellationToken cancellationToken = default);
}
