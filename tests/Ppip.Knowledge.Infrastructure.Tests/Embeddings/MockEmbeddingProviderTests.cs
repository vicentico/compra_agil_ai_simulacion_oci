using Microsoft.Extensions.Options;
using Ppip.Knowledge.Infrastructure.Embeddings;
using Xunit;

namespace Ppip.Knowledge.Infrastructure.Tests.Embeddings;

public class MockEmbeddingProviderTests
{
    private static MockEmbeddingProvider Build(int dimension = 768) =>
        new(Options.Create(new EmbeddingProviderOptions { Dimension = dimension }));

    [Fact]
    public async Task EmbedAsync_ReturnsVectorOfConfiguredDimension()
    {
        var provider = Build(dimension: 16);

        var vector = await provider.EmbedAsync("El plazo máximo de entrega es de 10 días hábiles.");

        Assert.Equal(16, vector.Values.Length);
        Assert.Equal(16, vector.Dimension);
        Assert.Equal("mock-embedding-v1", vector.ModelVersion);
    }

    [Fact]
    public async Task EmbedAsync_SameText_ProducesSameVector()
    {
        var provider = Build();

        var a = await provider.EmbedAsync("mismo texto");
        var b = await provider.EmbedAsync("mismo texto");

        Assert.Equal(a.Values, b.Values);
    }

    [Fact]
    public async Task EmbedAsync_DifferentText_ProducesDifferentVector()
    {
        var provider = Build();

        var a = await provider.EmbedAsync("texto uno");
        var b = await provider.EmbedAsync("texto dos");

        Assert.NotEqual(a.Values, b.Values);
    }

    [Fact]
    public async Task EmbedAsync_ReturnsNormalizedVector()
    {
        var provider = Build(dimension: 32);

        var vector = await provider.EmbedAsync("normalización");
        var norm = Math.Sqrt(vector.Values.Sum(v => (double)v * v));

        Assert.InRange(norm, 0.99, 1.01);
    }
}
