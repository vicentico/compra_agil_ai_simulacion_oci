using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Ppip.DocumentIntelligence.Infrastructure.Pdf;
using Xunit;

namespace Ppip.DocumentIntelligence.Infrastructure.Tests.Pdf;

/// <summary>
/// Contra PDFs reales generados con PdfSharpCore (no fixtures ChileCompra —
/// OQ-02 sigue abierta) — valida que la extracción de texto y la heurística
/// de tablas funcionan sobre el parser real, no un doble.
/// </summary>
public class PdfPigExtractorTests
{
    private readonly PdfPigExtractor _extractor = new();

    private static byte[] BuildTextPdf(string text)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Arial", 14);
        gfx.DrawString(text, font, XBrushes.Black, new XRect(0, 0, page.Width, page.Height), XStringFormats.TopLeft);

        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static byte[] BuildTableLikePdf()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Arial", 12);

        string[,] rows =
        {
            { "Producto", "Cantidad", "Precio" },
            { "Notebook", "10", "500000" },
            { "Mouse", "20", "8000" },
            { "Teclado", "15", "12000" },
        };

        for (var row = 0; row < rows.GetLength(0); row++)
        {
            var y = 80 + (row * 25);
            gfx.DrawString(rows[row, 0], font, XBrushes.Black, new XPoint(50, y));
            gfx.DrawString(rows[row, 1], font, XBrushes.Black, new XPoint(250, y));
            gfx.DrawString(rows[row, 2], font, XBrushes.Black, new XPoint(400, y));
        }

        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static byte[] BuildEmptyPdf()
    {
        using var document = new PdfDocument();
        document.AddPage();
        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    [Fact]
    public void Extract_TextualPdf_ReturnsNativeTextAndPositiveDensity()
    {
        var pdfBytes = BuildTextPdf("Esta es una compra ágil de notebooks para la oficina central.");

        var result = _extractor.Extract(pdfBytes);

        var page = Assert.Single(result.Pages);
        Assert.Equal(1, page.PageNumber);
        Assert.Contains("notebooks", page.NativeText);
        Assert.True(page.TextDensity > 0);
    }

    [Fact]
    public void Extract_EmptyPage_ReturnsZeroDensityAndNoTableLayout()
    {
        var pdfBytes = BuildEmptyPdf();

        var result = _extractor.Extract(pdfBytes);

        var page = Assert.Single(result.Pages);
        Assert.Equal(0d, page.TextDensity);
        Assert.False(page.HasTableLikeLayout);
        Assert.Empty(page.EmbeddedImages);
    }

    [Fact]
    public void Extract_TableLikeLayout_DetectsTable()
    {
        var pdfBytes = BuildTableLikePdf();

        var result = _extractor.Extract(pdfBytes);

        var page = Assert.Single(result.Pages);
        Assert.True(page.HasTableLikeLayout, "4 filas con 3 columnas alineadas deberían activar la heurística de tabla.");
    }

    [Fact]
    public void Extract_ProseText_DoesNotDetectTable()
    {
        var pdfBytes = BuildTextPdf("Este es un párrafo normal de prosa sin ninguna estructura tabular evidente en su contenido.");

        var result = _extractor.Extract(pdfBytes);

        var page = Assert.Single(result.Pages);
        Assert.False(page.HasTableLikeLayout);
    }

    [Fact]
    public void Extract_MultiplePages_ReturnsInOrder()
    {
        using var document = new PdfDocument();
        foreach (var text in new[] { "Página uno", "Página dos", "Página tres" })
        {
            var page = document.AddPage();
            using var gfx = XGraphics.FromPdfPage(page);
            gfx.DrawString(text, new XFont("Arial", 14), XBrushes.Black, new XPoint(50, 50));
        }

        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);

        var result = _extractor.Extract(stream.ToArray());

        Assert.Equal(3, result.Pages.Count);
        Assert.Equal([1, 2, 3], result.Pages.Select(p => p.PageNumber));
        Assert.Contains("uno", result.Pages[0].NativeText);
        Assert.Contains("tres", result.Pages[2].NativeText);
    }
}
