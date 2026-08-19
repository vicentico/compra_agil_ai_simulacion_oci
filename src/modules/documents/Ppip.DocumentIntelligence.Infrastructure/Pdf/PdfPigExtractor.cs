using Ppip.DocumentIntelligence.Domain.Ports;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Ppip.DocumentIntelligence.Infrastructure.Pdf;

/// <summary>
/// Adaptador real de <see cref="IPdfExtractor"/> sobre PdfPig — librería
/// PDF pura .NET (sin binarios nativos), docs/09-document-intelligence/01
/// §Clasificación: "Inspección con librería PDF (.NET)". <see cref="TableLayoutHeuristic"/>
/// implementa la detección de tablas explícitamente documentada como
/// heurística, no exacta.
/// </summary>
public sealed class PdfPigExtractor : IPdfExtractor
{
    public ExtractedPdf Extract(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        var pages = new List<ExtractedPage>();

        foreach (Page page in document.GetPages())
        {
            var text = page.Text ?? string.Empty;
            var area = page.Width * page.Height;
            var density = area > 0 ? text.Length / area : 0d;
            var words = page.GetWords().ToList();
            var hasTableLayout = TableLayoutHeuristic.Detect(words);
            var images = ExtractImages(page);

            pages.Add(new ExtractedPage(page.Number, text, density, hasTableLayout, images));
        }

        return new ExtractedPdf(pages);
    }

    // Solo PNG decodificable a propósito (no el fallback a bytes crudos
    // sin decodificar, p.ej. JPEG/DCTDecode tal cual) — lo que consume esto
    // (OCR, y "hasEmbeddedImages" para clasificación) necesita un formato
    // de imagen real, no un blob de bytes de formato ambiguo.
    private static IReadOnlyList<byte[]> ExtractImages(Page page)
    {
        var images = new List<byte[]>();
        foreach (var image in page.GetImages())
        {
            if (image.TryGetPng(out var pngBytes))
            {
                images.Add(pngBytes);
            }
        }

        return images;
    }
}
