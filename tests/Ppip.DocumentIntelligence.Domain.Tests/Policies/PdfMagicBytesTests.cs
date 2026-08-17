using System.Text;
using Ppip.DocumentIntelligence.Domain.Policies;
using Xunit;

namespace Ppip.DocumentIntelligence.Domain.Tests.Policies;

public class PdfMagicBytesTests
{
    [Fact]
    public void Matches_RealPdfHeader_ReturnsTrue()
    {
        var header = Encoding.ASCII.GetBytes("%PDF-1.7\n%âãÏÓ\n");

        Assert.True(PdfMagicBytes.Matches(header));
    }

    [Fact]
    public void Matches_HtmlPretendingToBePdf_ReturnsFalse()
    {
        var header = Encoding.ASCII.GetBytes("<!DOCTYPE html><html>");

        Assert.False(PdfMagicBytes.Matches(header));
    }

    [Fact]
    public void Matches_TooShort_ReturnsFalse()
    {
        var header = "%PD"u8.ToArray();

        Assert.False(PdfMagicBytes.Matches(header));
    }
}
