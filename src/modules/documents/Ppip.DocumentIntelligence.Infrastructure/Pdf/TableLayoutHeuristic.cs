using UglyToad.PdfPig.Content;

namespace Ppip.DocumentIntelligence.Infrastructure.Pdf;

/// <summary>
/// Heurística de layout para detectar tablas (docs/09-document-intelligence/01:
/// "Tablas detectadas por heurística de layout" — documentado explícitamente
/// como heurística, no exacta, FR-015). Agrupa palabras en líneas por
/// cercanía vertical; en cada línea, un salto horizontal se cuenta como
/// borde de columna si supera varias veces la altura del texto de esa línea
/// (proxy del tamaño de fuente) — un espacio normal entre palabras es una
/// fracción pequeña de esa altura, mientras que una celda de tabla
/// deliberadamente espaciada no. Declara tabla si suficientes líneas
/// consecutivas tienen ≥3 columnas.
///
/// Nota de diseño (encontrado probando contra un PDF real, no una
/// suposición): la primera versión comparaba cada salto contra la
/// <em>mediana de los saltos de esa misma línea</em> — falla exactamente
/// cuando TODOS los saltos de la línea son anchos de columna (el caso común
/// de una tabla real, ver test <c>Extract_TableLikeLayout_DetectsTable</c>),
/// porque entonces ningún salto se ve "anormal" respecto a los demás. El
/// umbral absoluto (relativo a la altura del texto, no a los otros saltos)
/// no tiene ese problema.
/// </summary>
internal static class TableLayoutHeuristic
{
    private const int MinAlignedLines = 3;
    private const int MinColumnsPerLine = 3;
    private const double LineYTolerance = 2.0;

    /// <summary>Un espacio normal entre palabras es una fracción del alto de línea; una celda de tabla separa mucho más — multiplicador conservador para no confundir prosa con espaciado generoso.</summary>
    private const double ColumnGapHeightMultiplier = 3.0;

    public static bool Detect(IReadOnlyList<Word> words)
    {
        if (words.Count == 0)
        {
            return false;
        }

        var lines = GroupIntoLines(words);
        var linesWithColumns = lines.Count(line => CountColumns(line) >= MinColumnsPerLine);
        return linesWithColumns >= MinAlignedLines;
    }

    private static List<List<Word>> GroupIntoLines(IReadOnlyList<Word> words)
    {
        var lines = new List<List<Word>>();
        foreach (var word in words.OrderByDescending(w => w.BoundingBox.Bottom))
        {
            var line = lines.Find(l => Math.Abs(l[0].BoundingBox.Bottom - word.BoundingBox.Bottom) <= LineYTolerance);
            if (line is null)
            {
                lines.Add([word]);
            }
            else
            {
                line.Add(word);
            }
        }

        foreach (var line in lines)
        {
            line.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));
        }

        return lines;
    }

    private static int CountColumns(List<Word> line)
    {
        if (line.Count < 2)
        {
            return line.Count;
        }

        var avgTextHeight = line.Average(w => w.BoundingBox.Top - w.BoundingBox.Bottom);
        var columnGapThreshold = avgTextHeight * ColumnGapHeightMultiplier;

        var columnBreaks = 0;
        for (var i = 1; i < line.Count; i++)
        {
            var gap = line[i].BoundingBox.Left - line[i - 1].BoundingBox.Right;
            if (gap > columnGapThreshold)
            {
                columnBreaks++;
            }
        }

        return columnBreaks + 1;
    }
}
