using System.Security.Cryptography;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Ocr;

/// <summary>
/// Implementación determinística de <see cref="IOcrService"/> (ADR-006:
/// "MockOcrService (fixtures determinísticos)") — sin dependencias nativas,
/// usada en tests y en Demo Mode offline (docs/16-operations). Nunca
/// devuelve el mismo texto para binarios distintos (hash del contenido en
/// el resultado) para poder distinguir páginas en tests/inspección manual.
/// </summary>
public sealed class MockOcrService : IOcrService
{
    private const double FixedConfidence = 0.90;

    public Task<OcrResult> RecognizeAsync(byte[] pageImage, CancellationToken cancellationToken = default)
    {
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(pageImage))[..12];
        return Task.FromResult(new OcrResult($"[texto OCR simulado — imagen {fingerprint}]", FixedConfidence));
    }
}
