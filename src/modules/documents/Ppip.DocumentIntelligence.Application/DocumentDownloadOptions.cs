namespace Ppip.DocumentIntelligence.Application;

/// <summary>
/// Config esperada: <c>Ppip:Documents:*</c>. El allowlist por defecto
/// (<c>*.mercadopublico.cl</c>) es la familia de dominios de ChileCompra —
/// confirmada por la propia URL base de la API (<c>api2.mercadopublico.cl</c>,
/// FASE 5), pero SIN confirmar que sea el dominio real de descarga de
/// adjuntos: OQ-02 sigue abierta (docs/01-discovery/09-open-questions.md) —
/// la Guía de Uso oficial de la API v2 no documenta ningún endpoint de
/// descarga. Configurable sin recompilar para cuando se confirme.
/// </summary>
public sealed class DocumentDownloadOptions
{
    public const string SectionName = "Ppip:Documents";

    public IReadOnlyList<string> AllowedUrlPatterns { get; set; } = ["*.mercadopublico.cl"];

    public IReadOnlyList<string> AllowedContentTypes { get; set; } = ["application/pdf"];

    /// <summary>ASM-04 (POC, volumen bajo): 50MB por defecto, generoso para un PDF de bases.</summary>
    public long MaxSizeBytes { get; set; } = 50 * 1024 * 1024;

    public string Bucket { get; set; } = "chilecompra";

    public string Producer { get; set; } = "document-worker@1.0.0";
}
