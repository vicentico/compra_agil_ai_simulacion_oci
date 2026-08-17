namespace Ppip.Procurement.Domain.Ports;

/// <summary>
/// Lock distribuido para evitar ciclos de sync concurrentes (UC-001 A5,
/// docs/08-data: <c>lock:sync:{source}</c> TTL 10m). Adaptador Redis real
/// (SETNX) en FASE 6.
/// </summary>
public interface ISyncLock
{
    /// <summary>
    /// Intenta adquirir el lock de <paramref name="source"/>. Devuelve
    /// <c>null</c> si ya está tomado (el caller debe terminar el ciclo como
    /// <c>Skipped</c>, nunca esperar/reintentar dentro del mismo ciclo).
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string source, TimeSpan ttl, CancellationToken cancellationToken = default);
}
