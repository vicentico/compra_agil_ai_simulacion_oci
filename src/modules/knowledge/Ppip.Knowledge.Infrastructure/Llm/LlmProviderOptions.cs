namespace Ppip.Knowledge.Infrastructure.Llm;

/// <summary>Config esperada: <c>Ppip:Knowledge:Llm:*</c> (FASE 9).</summary>
public sealed class LlmProviderOptions
{
    public const string SectionName = "Ppip:Knowledge:Llm";

    /// <summary><c>Mock</c> (por defecto, determinístico) o <c>Ollama</c> (real, no validado contra un modelo real en esta sesión).</summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Modelo Ollama a usar (docs/06-api/01-example-rag-query.md ejemplo: "llama3.1:8b").</summary>
    public string Model { get; set; } = "llama3.1:8b";
}
