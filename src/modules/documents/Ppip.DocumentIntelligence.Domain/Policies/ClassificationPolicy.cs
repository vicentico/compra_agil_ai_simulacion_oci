namespace Ppip.DocumentIntelligence.Domain.Policies;

/// <summary>
/// FR-012, docs/09-document-intelligence/01 §Clasificación: densidad de
/// texto por página decide textual/escaneado/mixto; tablas/imágenes son
/// refinamientos por heurística de layout — "Tablas detectadas por
/// heurística de layout" (documentado explícitamente como heurística, no
/// como algo exacto). Prioridad cuando compiten: complejo (tablas+imágenes)
/// &gt; tablas &gt; imágenes &gt; textual/mixto — un documento escaneado
/// nunca se reclasifica por tener "imágenes" (es, en esencia, todo imagen).
/// Puro: los umbrales vienen de configuración, nunca hardcodeados.
/// </summary>
public static class ClassificationPolicy
{
    public static DocumentClass Classify(
        IReadOnlyList<double> pageTextDensities,
        bool hasEmbeddedImages,
        bool hasDetectedTables,
        double textualDensityThreshold,
        double scannedDensityThreshold)
    {
        if (pageTextDensities.Count == 0)
        {
            throw new ArgumentException("El documento debe tener al menos una página.", nameof(pageTextDensities));
        }

        if (textualDensityThreshold <= scannedDensityThreshold)
        {
            throw new ArgumentException("El umbral textual debe ser mayor al umbral de escaneado.", nameof(textualDensityThreshold));
        }

        if (pageTextDensities.All(d => d <= scannedDensityThreshold))
        {
            return DocumentClass.Scanned;
        }

        if (hasDetectedTables && hasEmbeddedImages)
        {
            return DocumentClass.Complex;
        }

        if (hasDetectedTables)
        {
            return DocumentClass.Tables;
        }

        if (hasEmbeddedImages)
        {
            return DocumentClass.Images;
        }

        return pageTextDensities.All(d => d >= textualDensityThreshold) ? DocumentClass.Textual : DocumentClass.Mixed;
    }

    /// <summary>Una página va a OCR si su densidad cae en o bajo el umbral de "escaneado" (FR-014) — el resto conserva su texto nativo.</summary>
    public static bool RequiresOcr(double pageTextDensity, double scannedDensityThreshold) =>
        pageTextDensity <= scannedDensityThreshold;
}
