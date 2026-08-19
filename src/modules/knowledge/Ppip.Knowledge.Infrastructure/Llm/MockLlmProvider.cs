using System.Text.RegularExpressions;
using Ppip.Knowledge.Domain.Ports;

namespace Ppip.Knowledge.Infrastructure.Llm;

/// <summary>
/// Respuesta determinística: cita el primer fragmento de contexto numerado
/// que encuentra en el prompt (formato "[n] (documento: ...) texto", ver
/// <c>RagQueryOrchestrator.BuildUserPrompt</c>), o degrada al mensaje fijo de
/// UC-005 A1 si no hay ninguno. Proveedor por defecto — mismo criterio que
/// <c>MockOcrService</c>/<c>MockEmbeddingProvider</c>: ejercita el flujo de
/// citación → evidencia sin requerir un modelo real descargado en Ollama.
/// </summary>
public sealed partial class MockLlmProvider : ILlmProvider
{
    public Task<LlmCompletionResult> CompleteAsync(string systemPrompt, string userPrompt, LlmOptions options, CancellationToken cancellationToken = default)
    {
        var match = FirstContextMarker().Match(userPrompt);
        var text = match.Success
            ? $"Respuesta simulada (MockLlmProvider, sin modelo real configurado): ver evidencia citada [{match.Groups[1].Value}]."
            : "Información no encontrada en las fuentes analizadas.";

        var result = new LlmCompletionResult(text, "mock-llm-v1", TokensIn: userPrompt.Length / 4, TokensOut: text.Length / 4, LatencyMs: 5);
        return Task.FromResult(result);
    }

    [GeneratedRegex(@"^\[(\d+)\]", RegexOptions.Multiline)]
    private static partial Regex FirstContextMarker();
}
