namespace Ppip.DocumentIntelligence.Domain;

/// <summary>Cómo se obtuvo el texto de una <see cref="DocumentPage"/> — texto nativo del PDF, u OCR sobre su imagen renderizada.</summary>
public enum ExtractionMethod
{
    Textual,
    Ocr,
}
