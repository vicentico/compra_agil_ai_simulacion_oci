using Ppip.DocumentIntelligence.Domain.Policies;
using Xunit;

namespace Ppip.DocumentIntelligence.Domain.Tests.Policies;

public class UrlAllowlistPolicyTests
{
    private static readonly string[] AllowedPatterns = ["*.mercadopublico.cl"];

    [Fact]
    public void IsAllowed_HttpsSubdomainMatch_ReturnsTrue()
    {
        var url = new Uri("https://docs.mercadopublico.cl/bases.pdf");

        Assert.True(UrlAllowlistPolicy.IsAllowed(url, AllowedPatterns));
    }

    [Fact]
    public void IsAllowed_Http_ReturnsFalse()
    {
        var url = new Uri("http://docs.mercadopublico.cl/bases.pdf");

        Assert.False(UrlAllowlistPolicy.IsAllowed(url, AllowedPatterns));
    }

    [Fact]
    public void IsAllowed_UnrelatedDomain_ReturnsFalse()
    {
        var url = new Uri("https://attacker.example.com/bases.pdf");

        Assert.False(UrlAllowlistPolicy.IsAllowed(url, AllowedPatterns));
    }

    [Theory]
    [InlineData("https://mercadopublico.cl.attacker.com/bases.pdf")] // sufijo falso: "mercadopublico.cl" como prefijo, no como dominio real
    [InlineData("https://evilmercadopublico.cl/bases.pdf")] // sin el punto separador — no es un subdominio real
    public void IsAllowed_LookalikeDomain_ReturnsFalse(string maliciousUrl)
    {
        var url = new Uri(maliciousUrl);

        Assert.False(UrlAllowlistPolicy.IsAllowed(url, AllowedPatterns));
    }

    [Fact]
    public void IsAllowed_IpLiteral_ReturnsFalse()
    {
        var url = new Uri("https://169.254.169.254/latest/meta-data/");

        Assert.False(UrlAllowlistPolicy.IsAllowed(url, AllowedPatterns));
    }

    [Fact]
    public void IsAllowed_ExactHostPattern_MatchesOnlyExact()
    {
        string[] patterns = ["mercadopublico.cl"];

        Assert.True(UrlAllowlistPolicy.IsAllowed(new Uri("https://mercadopublico.cl/x.pdf"), patterns));
        Assert.False(UrlAllowlistPolicy.IsAllowed(new Uri("https://sub.mercadopublico.cl/x.pdf"), patterns));
    }
}
