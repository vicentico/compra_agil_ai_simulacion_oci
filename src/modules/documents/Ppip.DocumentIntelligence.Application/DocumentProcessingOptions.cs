namespace Ppip.DocumentIntelligence.Application;

/// <summary>
/// Config esperada: <c>Ppip:Documents:Processing:*</c> (FASE 8). Umbrales de
/// densidad **provisionales**: sin un corpus real de Compras Ágiles para
/// calibrarlos (OQ-02 sigue bloqueando descargas reales), estos valores son
/// un punto de partida razonable, no una medición — se recalibran en cuanto
/// existan PDFs reales procesados. Nunca hardcodeados en el código (docs/09).
/// </summary>
public sealed class DocumentProcessingOptions
{
    public const string SectionName = "Ppip:Documents:Processing";

    /// <summary>Caracteres extraíbles / puntos² de página. Igual o por encima: página textual.</summary>
    public double TextualDensityThreshold { get; set; } = 0.005;

    /// <summary>Igual o por debajo: página escaneada (y candidata a OCR).</summary>
    public double ScannedDensityThreshold { get; set; } = 0.001;

    public int TargetChunkTokens { get; set; } = 384;

    public int MaxChunkTokens { get; set; } = 512;

    public int ChunkOverlapTokens { get; set; } = 30;

    public string Producer { get; set; } = "document-worker@1.0.0";
}
