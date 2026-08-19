namespace Ppip.Knowledge.Domain.Ports;

/// <summary>
/// ADR-007 define el contrato canónico como CompleteStructuredAsync(promptRef,
/// context, schema, options) → JSON validado + usage. Para FASE 9 (un único
/// caso de uso: síntesis de respuesta RAG) se simplifica a texto crudo +
/// usage; el parseo/validación del JSON de salida vive en
/// Knowledge.Application. Revisitar el contrato completo si FASE 10
/// (AIAnalysis) necesita más de un `promptRef`.
/// </summary>
public interface ILlmProvider
{
    Task<LlmCompletionResult> CompleteAsync(string systemPrompt, string userPrompt, LlmOptions options, CancellationToken cancellationToken = default);
}

public sealed record LlmOptions(string PromptVersion, double Temperature = 0.1, int MaxOutputTokens = 1024);

public sealed record LlmCompletionResult(string RawText, string Model, int TokensIn, int TokensOut, long LatencyMs);
