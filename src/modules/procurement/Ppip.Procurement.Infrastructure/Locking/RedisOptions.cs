namespace Ppip.Procurement.Infrastructure.Locking;

/// <summary>Config esperada: <c>Ppip:Redis:ConnectionString</c> (ya usada por el health check desde FASE 1).</summary>
public sealed class RedisOptions
{
    public const string SectionName = "Ppip:Redis";

    public string ConnectionString { get; set; } = string.Empty;
}
