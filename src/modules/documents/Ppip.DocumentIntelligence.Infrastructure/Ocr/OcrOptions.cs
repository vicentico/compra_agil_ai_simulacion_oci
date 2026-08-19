namespace Ppip.DocumentIntelligence.Infrastructure.Ocr;

/// <summary>Config esperada: <c>Ppip:Ocr:*</c> (ADR-006: selección de proveedor sin tocar dominio).</summary>
public sealed class OcrOptions
{
    public const string SectionName = "Ppip:Ocr";

    /// <summary>"Mock" (default, sin dependencias nativas) o "Tesseract" (real, local — requiere tesseract-ocr instalado, ver Dockerfile).</summary>
    public string Provider { get; set; } = "Mock";

    public string TessDataPath { get; set; } = "./tessdata";

    /// <summary>ADR-006: "Tesseract (spa+eng)".</summary>
    public string Languages { get; set; } = "spa+eng";
}
