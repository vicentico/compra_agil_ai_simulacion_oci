using Ppip.DocumentIntelligence.Domain.Policies;
using Xunit;

namespace Ppip.DocumentIntelligence.Domain.Tests.Policies;

public class ClassificationPolicyTests
{
    private const double Textual = 0.01;
    private const double Scanned = 0.001;

    [Fact]
    public void Classify_AllPagesAboveTextualThreshold_ReturnsTextual()
    {
        var result = ClassificationPolicy.Classify([0.02, 0.015], hasEmbeddedImages: false, hasDetectedTables: false, Textual, Scanned);

        Assert.Equal(DocumentClass.Textual, result);
    }

    [Fact]
    public void Classify_AllPagesAtOrBelowScannedThreshold_ReturnsScanned()
    {
        var result = ClassificationPolicy.Classify([0.0005, 0.0001], hasEmbeddedImages: true, hasDetectedTables: true, Textual, Scanned);

        Assert.Equal(DocumentClass.Scanned, result);
    }

    [Fact]
    public void Classify_MixedDensities_ReturnsMixed()
    {
        var result = ClassificationPolicy.Classify([0.02, 0.0005], hasEmbeddedImages: false, hasDetectedTables: false, Textual, Scanned);

        Assert.Equal(DocumentClass.Mixed, result);
    }

    [Fact]
    public void Classify_TextualWithTables_ReturnsTables()
    {
        var result = ClassificationPolicy.Classify([0.02], hasEmbeddedImages: false, hasDetectedTables: true, Textual, Scanned);

        Assert.Equal(DocumentClass.Tables, result);
    }

    [Fact]
    public void Classify_TextualWithImages_ReturnsImages()
    {
        var result = ClassificationPolicy.Classify([0.02], hasEmbeddedImages: true, hasDetectedTables: false, Textual, Scanned);

        Assert.Equal(DocumentClass.Images, result);
    }

    [Fact]
    public void Classify_TablesAndImages_ReturnsComplex()
    {
        var result = ClassificationPolicy.Classify([0.02], hasEmbeddedImages: true, hasDetectedTables: true, Textual, Scanned);

        Assert.Equal(DocumentClass.Complex, result);
    }

    [Fact]
    public void Classify_ScannedTakesPriorityOverImagesAndTables()
    {
        // Un documento genuinamente escaneado no se reclasifica por tener
        // "imágenes" — cada página escaneada básicamente ES una imagen.
        var result = ClassificationPolicy.Classify([0.0001], hasEmbeddedImages: true, hasDetectedTables: true, Textual, Scanned);

        Assert.Equal(DocumentClass.Scanned, result);
    }

    [Fact]
    public void Classify_EmptyPageList_Throws()
    {
        Assert.Throws<ArgumentException>(() => ClassificationPolicy.Classify([], false, false, Textual, Scanned));
    }

    [Fact]
    public void Classify_InvertedThresholds_Throws()
    {
        Assert.Throws<ArgumentException>(() => ClassificationPolicy.Classify([0.01], false, false, textualDensityThreshold: 0.001, scannedDensityThreshold: 0.01));
    }

    [Theory]
    [InlineData(0.0005, true)]
    [InlineData(0.001, true)]
    [InlineData(0.002, false)]
    public void RequiresOcr_ComparesAgainstScannedThreshold(double density, bool expected)
    {
        Assert.Equal(expected, ClassificationPolicy.RequiresOcr(density, Scanned));
    }
}
