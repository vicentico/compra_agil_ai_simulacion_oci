namespace Ppip.BuildingBlocks.Messaging;

/// <summary>
/// Puerto de outbox — la aplicación depende de esto, nunca de MongoDB
/// directamente (NFR-013). El adaptador Mongo real (colección
/// <c>outbox_messages</c> + índice por <c>PublishedAt</c>) llega en FASE 6
/// cuando Sync Worker publique el primer evento real.
/// </summary>
public interface IOutboxStore
{
    Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default);
}
