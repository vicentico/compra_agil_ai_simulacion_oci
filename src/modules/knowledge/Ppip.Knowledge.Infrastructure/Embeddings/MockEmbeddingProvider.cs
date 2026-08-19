using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Ppip.Knowledge.Domain.Ports;

namespace Ppip.Knowledge.Infrastructure.Embeddings;

/// <summary>
/// Vector determinístico (SHA-256 del texto, expandido y normalizado) — sin
/// dependencias externas. Proveedor por defecto (<c>Ppip:Knowledge:Embeddings:Provider=Mock</c>),
/// mismo criterio que <c>MockOcrService</c> (FASE 8): permite correr el
/// pipeline completo sin requerir Ollama con un modelo real descargado.
/// </summary>
public sealed class MockEmbeddingProvider(IOptions<EmbeddingProviderOptions> options) : IEmbeddingProvider
{
    public Task<EmbeddingVector> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var dimension = options.Value.Dimension;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var values = new float[dimension];
        for (var i = 0; i < dimension; i++)
        {
            values[i] = (hash[i % hash.Length] / 255f * 2f) - 1f;
        }

        Normalize(values);
        return Task.FromResult(new EmbeddingVector(values, "mock-embedding-v1", dimension));
    }

    private static void Normalize(float[] values)
    {
        var norm = MathF.Sqrt(values.Sum(v => v * v));
        if (norm <= 0f)
        {
            return;
        }

        for (var i = 0; i < values.Length; i++)
        {
            values[i] /= norm;
        }
    }
}
