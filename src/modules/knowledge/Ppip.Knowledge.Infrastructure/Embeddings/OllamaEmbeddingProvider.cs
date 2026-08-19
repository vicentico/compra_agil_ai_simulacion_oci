using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ppip.Knowledge.Domain.Exceptions;
using Ppip.Knowledge.Domain.Ports;

namespace Ppip.Knowledge.Infrastructure.Embeddings;

/// <summary>
/// Adaptador real contra Ollama (<c>POST /api/embeddings</c>, OQ-03: nomic-embed-text).
/// Implementado pero NO ejercido contra un modelo real descargado en esta
/// sesión (mismo criterio que <c>TesseractOcrService</c>, FASE 8) — el
/// pipeline valida su forma vía WireMock; el default de producción es
/// <see cref="MockEmbeddingProvider"/>.
/// </summary>
public sealed class OllamaEmbeddingProvider(HttpClient httpClient, IOptions<EmbeddingProviderOptions> options) : IEmbeddingProvider
{
    public async Task<EmbeddingVector> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        try
        {
            var request = new OllamaEmbedRequest(opts.ModelVersion, text);
            using var response = await httpClient.PostAsJsonAsync("/api/embeddings", request, JsonSerializerOptions.Web, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(JsonSerializerOptions.Web, cancellationToken);
            if (body?.Embedding is not { Length: > 0 } embedding)
            {
                throw new RetrievalUnavailableException("Ollama devolvió una respuesta vacía al generar el embedding.");
            }

            return new EmbeddingVector(embedding, opts.ModelVersion, embedding.Length);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new RetrievalUnavailableException("No fue posible contactar a Ollama para generar el embedding.", ex);
        }
    }

    private sealed record OllamaEmbedRequest(string Model, string Prompt);

    private sealed record OllamaEmbedResponse(float[]? Embedding);
}
