namespace Ppip.Procurement.Application;

/// <summary>Config esperada: <c>Ppip:Sync:*</c> — parámetros del ciclo de sincronización (UC-001).</summary>
public sealed class SyncOptions
{
    public const string SectionName = "Ppip:Sync";

    /// <summary>Nombre de la fuente — identidad del <see cref="Domain.SyncCheckpoint"/> singleton y del lock distribuido.</summary>
    public string Source { get; set; } = "chilecompra";

    /// <summary>Cuánto retrocede la ventana en el primer ciclo (sin checkpoint previo).</summary>
    public TimeSpan InitialWindowLookback { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Tamaño de página hacia ChileCompra (10-50, ver <c>CompraAgilListQuery.Validate</c>).</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>TTL del lock distribuido (docs/08-data: <c>lock:sync:{source}</c> TTL 10m).</summary>
    public TimeSpan LockTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Identificador de productor para el envelope de eventos (docs/07-events/00).</summary>
    public string Producer { get; set; } = "sync-worker@1.0.0";

    /// <summary>Intervalo entre ciclos automáticos del scheduler (0 = deshabilitado, solo disparo manual).</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(15);
}
