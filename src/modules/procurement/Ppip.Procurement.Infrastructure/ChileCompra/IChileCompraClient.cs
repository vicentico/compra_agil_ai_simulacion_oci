using Ppip.Procurement.Infrastructure.ChileCompra.Dto;

namespace Ppip.Procurement.Infrastructure.ChileCompra;

/// <summary>
/// Puerto hacia la API Compra Ágil v2 (ADR-001: anti-corruption layer frente
/// a ChileCompra). Devuelve DTOs tal como los expone la API — la traducción
/// a agregados de <c>Ppip.Procurement.Domain</c> es responsabilidad de la
/// capa de aplicación (FASE 6, <c>SyncOrchestrator</c>), no de este cliente.
/// </summary>
public interface IChileCompraClient
{
    Task<CompraAgilListPayloadDto> ListarAsync(CompraAgilListQuery query, CancellationToken cancellationToken = default);

    Task<CompraAgilDetailDto> ObtenerDetalleAsync(string codigo, CancellationToken cancellationToken = default);
}
