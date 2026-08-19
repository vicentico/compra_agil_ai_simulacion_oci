namespace Ppip.DocumentIntelligence.Domain.Ports;

/// <summary>
/// Puerto OCR (ADR-006): implementación intercambiable sin tocar dominio —
/// <c>MockOcrService</c> (fixtures determinísticos, tests/demo offline) o
/// <c>TesseractOcrService</c> (real, local, spa+eng) en
/// <c>Ppip.DocumentIntelligence.Infrastructure</c>; <c>CloudOcrService</c>
/// (OCI Document Understanding) queda FUTURE explícito.
/// </summary>
public interface IOcrService
{
    Task<OcrResult> RecognizeAsync(byte[] pageImage, CancellationToken cancellationToken = default);
}

/// <summary><paramref name="Confidence"/> normalizada [0..1] (ADR-006).</summary>
public sealed record OcrResult(string Text, double Confidence);
