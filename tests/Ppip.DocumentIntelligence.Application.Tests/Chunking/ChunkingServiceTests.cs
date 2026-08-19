using Ppip.DocumentIntelligence.Application.Chunking;
using Ppip.DocumentIntelligence.Domain;
using Xunit;

namespace Ppip.DocumentIntelligence.Application.Tests.Chunking;

public class ChunkingServiceTests
{
    private static readonly ChunkingThresholds Generous = new(TargetChunkTokens: 200, MaxChunkTokens: 300, ChunkOverlapTokens: 10);

    private static DocumentPage Page(int number, string text) => DocumentPage.FromNativeText(number, text, textDensity: 0.02);

    [Fact]
    public void Chunk_SectionHeader_CreatesTitleChunkAndSetsSectionContext()
    {
        var pages = new[] { Page(1, "1. Objeto de la compra\n\nSe requiere adquirir notebooks para la oficina.") };

        var chunks = ChunkingService.Chunk(pages, Generous);

        Assert.Contains(chunks, c => c.ChunkType == ChunkType.Title && c.Text == "1. Objeto de la compra");
        var paragraph = Assert.Single(chunks, c => c.ChunkType == ChunkType.Paragraph);
        Assert.Equal("1. Objeto de la compra", paragraph.Section);
    }

    [Fact]
    public void Chunk_SubSectionHeader_SetsSubSectionUnderLastSection()
    {
        var pages = new[] { Page(1, "1. Requisitos\n\n1.1 Requisitos técnicos\n\nEl oferente debe cumplir con lo siguiente.") };

        var chunks = ChunkingService.Chunk(pages, Generous);

        var paragraph = Assert.Single(chunks, c => c.ChunkType == ChunkType.Paragraph);
        Assert.Equal("1. Requisitos", paragraph.Section);
        Assert.Equal("1.1 Requisitos técnicos", paragraph.SubSection);
    }

    [Fact]
    public void Chunk_RequirementLanguage_ClassifiesAsRequirement()
    {
        var pages = new[] { Page(1, "El oferente deberá presentar boleta de garantía dentro de 5 días hábiles.") };

        var chunks = ChunkingService.Chunk(pages, Generous);

        var chunk = Assert.Single(chunks);
        Assert.Equal(ChunkType.Requirement, chunk.ChunkType);
    }

    [Fact]
    public void Chunk_ListItems_ClassifiesAsList()
    {
        var pages = new[] { Page(1, "- Notebook 15 pulgadas\n- Mouse óptico\n- Teclado USB") };

        var chunks = ChunkingService.Chunk(pages, Generous);

        var chunk = Assert.Single(chunks);
        Assert.Equal(ChunkType.List, chunk.ChunkType);
    }

    [Fact]
    public void Chunk_TableLikeRows_ClassifiesAsTable()
    {
        var pages = new[] { Page(1, "Producto        Cantidad        Precio\nNotebook        10              500000\nMouse           20              8000") };

        var chunks = ChunkingService.Chunk(pages, Generous);

        var chunk = Assert.Single(chunks);
        Assert.Equal(ChunkType.Table, chunk.ChunkType);
    }

    [Fact]
    public void Chunk_MultipleSmallParagraphs_MergeUpToTarget()
    {
        var pages = new[] { Page(1, "Primer párrafo corto.\n\nSegundo párrafo también corto.\n\nTercer párrafo.") };
        var thresholds = new ChunkingThresholds(TargetChunkTokens: 100, MaxChunkTokens: 200, ChunkOverlapTokens: 5);

        var chunks = ChunkingService.Chunk(pages, thresholds);

        var chunk = Assert.Single(chunks);
        Assert.Contains("Primer párrafo", chunk.Text);
        Assert.Contains("Tercer párrafo", chunk.Text);
    }

    [Fact]
    public void Chunk_ParagraphExceedingMax_SplitsWithOverlap()
    {
        var words = string.Join(' ', Enumerable.Range(1, 50).Select(i => $"palabra{i}"));
        var pages = new[] { Page(1, words) };
        var thresholds = new ChunkingThresholds(TargetChunkTokens: 20, MaxChunkTokens: 20, ChunkOverlapTokens: 5);

        var chunks = ChunkingService.Chunk(pages, thresholds);

        Assert.True(chunks.Count > 1, "Un párrafo de 50 palabras con máximo 20 debe partirse en más de un chunk.");
        // El overlap significa que la última palabra de un chunk reaparece cerca del inicio del siguiente.
        var firstChunkWords = chunks[0].Text.Split(' ');
        var secondChunkWords = chunks[1].Text.Split(' ');
        Assert.Contains(firstChunkWords[^1], secondChunkWords);
    }

    [Fact]
    public void Chunk_NeverSpansMultiplePages()
    {
        var pages = new[] { Page(1, "Texto de la página uno."), Page(2, "Texto de la página dos.") };

        var chunks = ChunkingService.Chunk(pages, Generous);

        Assert.Equal(2, chunks.Count);
        Assert.Equal(1, chunks[0].Page);
        Assert.Equal(2, chunks[1].Page);
    }

    [Fact]
    public void Chunk_EmptyPage_ProducesNoChunks()
    {
        var pages = new[] { Page(1, string.Empty) };

        var chunks = ChunkingService.Chunk(pages, Generous);

        Assert.Empty(chunks);
    }
}
