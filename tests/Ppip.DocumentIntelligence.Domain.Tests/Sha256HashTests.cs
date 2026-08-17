using Ppip.DocumentIntelligence.Domain;
using Xunit;

namespace Ppip.DocumentIntelligence.Domain.Tests;

public class Sha256HashTests
{
    [Fact]
    public void From_ValidHash_NormalizesToLowercase()
    {
        var uppercase = new string('A', 64);

        var hash = Sha256Hash.From(uppercase);

        Assert.Equal(64, hash.Value.Length);
        Assert.Equal(new string('a', 64), hash.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void From_InvalidHash_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Sha256Hash.From(value));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var a = Sha256Hash.From(new string('a', 64));
        var b = Sha256Hash.From(new string('a', 64));

        Assert.Equal(a, b);
    }
}
