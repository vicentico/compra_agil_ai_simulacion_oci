namespace Ppip.BuildingBlocks.Messaging;

/// <summary>
/// Puerto de dedupe por <c>eventId</c> (docs/07-events/00, regla 2: "Redis
/// SETNX + unique index"; docs/14-reliability/01: TTL 7d). El adaptador Redis
/// real llega cuando exista el primer consumidor de eventos (FASE 6).
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Intenta marcar <paramref name="key"/> como procesada. Devuelve
    /// <c>true</c> la primera vez (el caller debe procesar) y <c>false</c> si
    /// ya estaba marcada (dedupe — el caller debe omitir el efecto).
    /// </summary>
    Task<bool> TryMarkProcessedAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default);
}
