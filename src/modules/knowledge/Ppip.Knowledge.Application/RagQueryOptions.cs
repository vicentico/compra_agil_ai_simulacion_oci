namespace Ppip.Knowledge.Application;

/// <summary>
/// Config esperada: <c>Ppip:Knowledge:Rag:*</c> (FASE 9, docs/10-rag/01-rag-specification.md).
/// </summary>
public sealed class RagQueryOptions
{
    public const string SectionName = "Ppip:Knowledge:Rag";

    /// <summary>k default (docs/10-rag/01: "top-k vectorial filtrado; k default 8 (1..20)").</summary>
    public int DefaultTopK { get; set; } = 8;

    public int MinTopK { get; set; } = 1;

    public int MaxTopK { get; set; } = 20;

    public int MinQuestionLength { get; set; } = 3;

    public int MaxQuestionLength { get; set; } = 1000;

    /// <summary>Score coseno mínimo para considerar un chunk relevante (UC-005 A1: bajo umbral → sin evidencia).</summary>
    public double MinScoreThreshold { get; set; } = 0.5;

    public string PromptVersion { get; set; } = "rag-answer-v1.0";

    public double Temperature { get; set; } = 0.1;

    public int MaxOutputTokens { get; set; } = 1024;

    public string Producer { get; set; } = "platform-api@1.0.0";
}
