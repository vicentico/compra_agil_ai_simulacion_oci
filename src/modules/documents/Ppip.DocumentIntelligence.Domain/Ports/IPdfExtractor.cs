namespace Ppip.DocumentIntelligence.Domain.Ports;

/// <summary>
/// Puerto de extracción PDF (NFR-013): la aplicación depende de esto, nunca
/// de una librería de parseo PDF directamente. Puro/sin I/O — todo el
/// binario ya está en memoria. Adaptador real (FASE 8, PdfPig) en
/// <c>Ppip.DocumentIntelligence.Infrastructure</c>.
/// </summary>
public interface IPdfExtractor
{
    ExtractedPdf Extract(byte[] pdfBytes);
}

public sealed record ExtractedPdf(IReadOnlyList<ExtractedPage> Pages);

/// <summary>
/// <paramref name="TextDensity"/> = caracteres extraíbles / área de la
/// página (docs/09-document-intelligence/01). <paramref name="HasTableLikeLayout"/>
/// es una heurística de alineación de columnas de texto — nunca 100%
/// precisa, documentado explícitamente como heurística en la especificación.
/// <paramref name="EmbeddedImages"/> son las imágenes rasterizadas embebidas
/// en la página tal cual (no una renderización del contenido vectorial/texto
/// de la página — ver ADR-006 y la nota de cierre de FASE 8 para el alcance
/// exacto de esta limitación).
/// </summary>
public sealed record ExtractedPage(
    int PageNumber,
    string NativeText,
    double TextDensity,
    bool HasTableLikeLayout,
    IReadOnlyList<byte[]> EmbeddedImages);
