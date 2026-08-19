using Xunit;

namespace Ppip.DocumentIntelligence.Domain.Tests;

public class DocumentPageTests
{
    [Fact]
    public void FromNativeText_Valid_SetsTextualExtractionMethod()
    {
        var page = DocumentPage.FromNativeText(1, "texto", textDensity: 0.02);

        Assert.Equal(ExtractionMethod.Textual, page.ExtractionMethod);
        Assert.Null(page.OcrConfidence);
        Assert.Equal("texto", page.Text);
    }

    [Fact]
    public void FromNativeText_InvalidPageNumber_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentPage.FromNativeText(0, "texto", 0.02));
    }

    [Fact]
    public void ApplyOcr_ReplacesTextAndSetsConfidence()
    {
        var page = DocumentPage.FromNativeText(1, string.Empty, textDensity: 0.0001);

        page.ApplyOcr("texto reconocido", 0.75);

        Assert.Equal("texto reconocido", page.Text);
        Assert.Equal(ExtractionMethod.Ocr, page.ExtractionMethod);
        Assert.Equal(0.75, page.OcrConfidence);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ApplyOcr_ConfidenceOutOfRange_Throws(double confidence)
    {
        var page = DocumentPage.FromNativeText(1, string.Empty, 0.0001);

        Assert.Throws<ArgumentOutOfRangeException>(() => page.ApplyOcr("texto", confidence));
    }

    [Fact]
    public void Rehydrate_PreservesAllFields()
    {
        var id = Guid.CreateVersion7();

        var page = DocumentPage.Rehydrate(id, 3, "texto", ExtractionMethod.Ocr, 0.0005, 0.6);

        Assert.Equal(id, page.Id);
        Assert.Equal(3, page.PageNumber);
        Assert.Equal(ExtractionMethod.Ocr, page.ExtractionMethod);
        Assert.Equal(0.6, page.OcrConfidence);
    }
}
