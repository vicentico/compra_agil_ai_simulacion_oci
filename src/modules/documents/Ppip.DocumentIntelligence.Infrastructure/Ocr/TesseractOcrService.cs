using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Domain.Ports;
using Tesseract;

namespace Ppip.DocumentIntelligence.Infrastructure.Ocr;

/// <summary>
/// Adaptador real de <see cref="IOcrService"/> (ADR-006) — requiere el
/// binario nativo de Tesseract + datos de idioma (spa+eng) instalados en el
/// entorno (ver Dockerfile de <c>Ppip.DocumentWorker</c>, que instala
/// <c>tesseract-ocr</c> vía apt). <see cref="TesseractEngine"/> no es
/// thread-safe para llamadas concurrentes sobre la misma instancia — se
/// serializan con un semáforo en vez de crear un engine por request (más
/// caro: cada instancia carga los datos de idioma desde disco).
/// </summary>
public sealed class TesseractOcrService : IOcrService, IDisposable
{
    private readonly TesseractEngine _engine;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public TesseractOcrService(IOptions<OcrOptions> options)
    {
        var opts = options.Value;
        _engine = new TesseractEngine(opts.TessDataPath, opts.Languages, EngineMode.Default);
    }

    public async Task<OcrResult> RecognizeAsync(byte[] pageImage, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                using var pix = Pix.LoadFromMemory(pageImage);
                using var page = _engine.Process(pix);
                return new OcrResult(page.GetText(), page.GetMeanConfidence());
            }, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _engine.Dispose();
        _gate.Dispose();
    }
}
