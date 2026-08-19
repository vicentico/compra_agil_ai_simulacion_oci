using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Ppip.Knowledge.Domain.Exceptions;
using Ppip.Knowledge.Domain.Ports;

namespace Ppip.Knowledge.Infrastructure.Llm;

/// <summary>
/// Adaptador real contra Ollama (<c>POST /api/generate</c>, sin streaming).
/// Implementado pero NO ejercido contra un modelo real descargado en esta
/// sesión (mismo criterio que <c>TesseractOcrService</c>, FASE 8) — validado
/// vía WireMock; el default de producción es <see cref="MockLlmProvider"/>.
/// </summary>
public sealed class OllamaLlmProvider(HttpClient httpClient, IOptions<LlmProviderOptions> options) : ILlmProvider
{
    public async Task<LlmCompletionResult> CompleteAsync(string systemPrompt, string userPrompt, LlmOptions llmOptions, CancellationToken cancellationToken = default)
    {
        var model = options.Value.Model;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var request = new OllamaGenerateRequest(
                model,
                systemPrompt,
                userPrompt,
                Stream: false,
                new OllamaGenerateParameters(llmOptions.Temperature, llmOptions.MaxOutputTokens));

            using var response = await httpClient.PostAsJsonAsync("/api/generate", request, JsonSerializerOptions.Web, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonSerializerOptions.Web, cancellationToken);
            if (body?.Response is not { Length: > 0 } text)
            {
                throw new LlmUnavailableException("Ollama devolvió una respuesta vacía.");
            }

            stopwatch.Stop();
            return new LlmCompletionResult(text, model, body.PromptEvalCount ?? 0, body.EvalCount ?? 0, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new LlmUnavailableException("No fue posible contactar a Ollama para generar la respuesta.", ex);
        }
    }

    private sealed record OllamaGenerateRequest(string Model, string System, string Prompt, bool Stream, OllamaGenerateParameters Options);

    private sealed record OllamaGenerateParameters(double Temperature, [property: JsonPropertyName("num_predict")] int NumPredict);

    private sealed record OllamaGenerateResponse(
        string? Response,
        [property: JsonPropertyName("prompt_eval_count")] int? PromptEvalCount,
        [property: JsonPropertyName("eval_count")] int? EvalCount);
}
