using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>
/// Checkpoint singleton por fuente (FR-005, UC-001): permite retomar la
/// sincronización incremental sin duplicar ni perder registros tras un
/// reinicio del worker. Id = nombre de la fuente (p.ej. "chilecompra").
/// </summary>
public sealed class SyncCheckpoint : Entity<string>
{
    public DateTimeOffset? LastSuccessfulSync { get; private set; }
    public DateTimeOffset WindowStart { get; private set; }
    public DateTimeOffset WindowEnd { get; private set; }

    private SyncCheckpoint(string source, DateTimeOffset windowStart, DateTimeOffset windowEnd) : base(source)
    {
        WindowStart = windowStart;
        WindowEnd = windowEnd;
    }

    public static SyncCheckpoint Initial(string source, DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("La fuente es obligatoria.", nameof(source));
        }

        if (windowEnd < windowStart)
        {
            throw new ArgumentException("La ventana de sincronización es inválida.");
        }

        return new SyncCheckpoint(source.Trim(), windowStart, windowEnd);
    }

    /// <summary>Avanza el checkpoint solo tras un ciclo exitoso (nunca antes — UC-001 postcondición).</summary>
    public void Advance(DateTimeOffset syncedAt, DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        if (windowEnd < windowStart)
        {
            throw new ArgumentException("La ventana de sincronización es inválida.");
        }

        LastSuccessfulSync = syncedAt;
        WindowStart = windowStart;
        WindowEnd = windowEnd;
    }
}
