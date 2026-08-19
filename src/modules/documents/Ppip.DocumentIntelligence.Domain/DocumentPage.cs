using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>
/// Una página extraída de una <see cref="DocumentVersion"/> (docs/03-domain/02).
/// El texto puede reemplazarse una vez si la página pasa por OCR (empieza
/// como texto nativo vacío/pobre, OCR lo completa) — fuera de ese caso, no
/// se vuelve a modificar.
/// </summary>
public sealed class DocumentPage : Entity<Guid>
{
    public int PageNumber { get; }
    public string Text { get; private set; }
    public ExtractionMethod ExtractionMethod { get; private set; }
    public double TextDensity { get; }
    public double? OcrConfidence { get; private set; }

    private DocumentPage(Guid id, int pageNumber, string text, ExtractionMethod extractionMethod, double textDensity, double? ocrConfidence)
        : base(id)
    {
        PageNumber = pageNumber;
        Text = text;
        ExtractionMethod = extractionMethod;
        TextDensity = textDensity;
        OcrConfidence = ocrConfidence;
    }

    public static DocumentPage FromNativeText(int pageNumber, string text, double textDensity)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "El número de página debe ser mayor o igual a 1.");
        }

        return new DocumentPage(Guid.CreateVersion7(), pageNumber, text ?? string.Empty, ExtractionMethod.Textual, textDensity, ocrConfidence: null);
    }

    /// <summary>Reemplaza el texto de una página de baja densidad con el resultado de OCR (UC-003 paso 6, FR-014) — la única vez que <see cref="Text"/> cambia.</summary>
    public void ApplyOcr(string text, double confidence)
    {
        if (confidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "La confianza OCR debe estar entre 0 y 1.");
        }

        Text = text ?? string.Empty;
        ExtractionMethod = ExtractionMethod.Ocr;
        OcrConfidence = confidence;
    }

    /// <summary>Usado por los repositorios para reconstruir desde almacenamiento.</summary>
    public static DocumentPage Rehydrate(Guid id, int pageNumber, string text, ExtractionMethod extractionMethod, double textDensity, double? ocrConfidence) =>
        new(id, pageNumber, text, extractionMethod, textDensity, ocrConfidence);
}
