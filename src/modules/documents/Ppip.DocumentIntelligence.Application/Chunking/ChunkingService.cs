using System.Text.RegularExpressions;
using Ppip.DocumentIntelligence.Domain;

namespace Ppip.DocumentIntelligence.Application.Chunking;

/// <summary>
/// Chunking semántico (FR-016, docs/09-document-intelligence/01 §Chunking
/// semántico): títulos/secciones → subsecciones → párrafos → requisitos →
/// listas → tablas. Puro: no conoce documentId/versionId/persistencia — el
/// orquestador traduce cada <see cref="PendingChunk"/> a un
/// <see cref="DocumentChunk"/> real. Simplificación deliberada de FASE 8: un
/// chunk nunca cruza el límite de página (evita ambigüedad de a qué página
/// asignar un chunk fusionado) — documentado como límite conocido, no oculto.
/// El conteo de tokens es una aproximación por palabras, no un tokenizer
/// real de ningún proveedor (ese detalle es de FASE 9, cuando se elija el
/// modelo de embeddings — OQ-03).
/// </summary>
public static class ChunkingService
{
    // "1", "1.2", "1.2.3" al inicio de línea, seguido de texto — heurística
    // de detección de encabezados de sección numerada.
    private static readonly Regex SectionHeaderPattern = new(@"^(?<number>\d+(?:\.\d+)*)[\.\)]?\s+(?<title>\S.*)$", RegexOptions.Compiled);
    private static readonly Regex RequirementPattern = new(@"\b(deber[áa]n?|se exige|es obligatorio|obligatoriamente)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ListItemPattern = new(@"^\s*([-•*]|\d+[\.\)])\s+\S", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex TableRowPattern = new(@"\S+(\s{2,}|\t)\S+(\s{2,}|\t)\S+", RegexOptions.Compiled);

    public static IReadOnlyList<PendingChunk> Chunk(IReadOnlyList<DocumentPage> pages, ChunkingThresholds thresholds)
    {
        var result = new List<PendingChunk>();
        string? currentSection = null;
        string? currentSubSection = null;

        foreach (var page in pages.OrderBy(p => p.PageNumber))
        {
            var buffer = new List<string>();
            void FlushParagraphBuffer()
            {
                if (buffer.Count == 0)
                {
                    return;
                }

                foreach (var merged in MergeToTargetSize(buffer, thresholds))
                {
                    result.Add(new PendingChunk(page.PageNumber, currentSection, currentSubSection, ChunkType.Paragraph, merged, EstimateTokens(merged)));
                }

                buffer.Clear();
            }

            foreach (var raw in SplitParagraphs(page.Text))
            {
                var text = raw.Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                var header = SectionHeaderPattern.Match(text);
                if (header.Success && text.Length <= 120)
                {
                    FlushParagraphBuffer();
                    if (header.Groups["number"].Value.Contains('.'))
                    {
                        currentSubSection = text;
                    }
                    else
                    {
                        currentSection = text;
                        currentSubSection = null;
                    }

                    result.Add(new PendingChunk(page.PageNumber, currentSection, currentSubSection, ChunkType.Title, text, EstimateTokens(text)));
                    continue;
                }

                var type = Classify(text);
                if (type == ChunkType.Paragraph)
                {
                    buffer.Add(text);
                    continue;
                }

                FlushParagraphBuffer();
                foreach (var piece in SplitIfTooLarge(text, thresholds))
                {
                    result.Add(new PendingChunk(page.PageNumber, currentSection, currentSubSection, type, piece, EstimateTokens(piece)));
                }
            }

            FlushParagraphBuffer();
        }

        return result;
    }

    private static ChunkType Classify(string text)
    {
        if (RequirementPattern.IsMatch(text))
        {
            return ChunkType.Requirement;
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length > 0 && lines.Count(ListItemPattern.IsMatch) >= Math.Max(1, lines.Length - 1))
        {
            return ChunkType.List;
        }

        if (lines.Length >= 2 && lines.Count(l => TableRowPattern.IsMatch(l)) >= Math.Min(2, lines.Length))
        {
            return ChunkType.Table;
        }

        return ChunkType.Paragraph;
    }

    private static IEnumerable<string> SplitParagraphs(string text) =>
        Regex.Split(text ?? string.Empty, @"\n\s*\n");

    private static IEnumerable<string> MergeToTargetSize(IReadOnlyList<string> paragraphs, ChunkingThresholds thresholds)
    {
        var current = new List<string>();
        var currentTokens = 0;

        foreach (var paragraph in paragraphs)
        {
            foreach (var piece in SplitIfTooLarge(paragraph, thresholds))
            {
                var pieceTokens = EstimateTokens(piece);
                if (currentTokens > 0 && currentTokens + pieceTokens > thresholds.TargetChunkTokens)
                {
                    yield return string.Join("\n\n", current);
                    current.Clear();
                    currentTokens = 0;
                }

                current.Add(piece);
                currentTokens += pieceTokens;
            }
        }

        if (current.Count > 0)
        {
            yield return string.Join("\n\n", current);
        }
    }

    /// <summary>Un bloque que por sí solo excede el máximo se corta por palabras con un pequeño overlap — el único caso donde el chunking usa solapamiento (docs/09: "overlap pequeño solo cuando el corte semántico excede el máximo").</summary>
    private static IEnumerable<string> SplitIfTooLarge(string text, ChunkingThresholds thresholds)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= thresholds.MaxChunkTokens)
        {
            yield return text;
            yield break;
        }

        var start = 0;
        while (start < words.Length)
        {
            var count = Math.Min(thresholds.MaxChunkTokens, words.Length - start);
            yield return string.Join(' ', words.Skip(start).Take(count));
            if (start + count >= words.Length)
            {
                yield break;
            }

            start += Math.Max(1, count - thresholds.ChunkOverlapTokens);
        }
    }

    private static int EstimateTokens(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}

public sealed record ChunkingThresholds(int TargetChunkTokens, int MaxChunkTokens, int ChunkOverlapTokens);

public sealed record PendingChunk(int Page, string? Section, string? SubSection, ChunkType ChunkType, string Text, int TokenCount);
