using Ppip.Knowledge.Domain.Ports;
using Ppip.Knowledge.Infrastructure.Llm;
using Xunit;

namespace Ppip.Knowledge.Infrastructure.Tests.Llm;

public class MockLlmProviderTests
{
    private readonly MockLlmProvider _provider = new();

    [Fact]
    public async Task CompleteAsync_ContextWithMarkers_CitesFirstOne()
    {
        var userPrompt = """
            Contexto:
            [1] (documento: Bases.pdf, página 7) El proveedor deberá entregar en 10 días hábiles.

            Pregunta: ¿Cuál es el plazo de entrega?
            """;

        var result = await _provider.CompleteAsync("system", userPrompt, new LlmOptions("rag-answer-v1.0"));

        Assert.Contains("[1]", result.RawText);
        Assert.Equal("mock-llm-v1", result.Model);
    }

    [Fact]
    public async Task CompleteAsync_NoContextMarkers_ReturnsNotFoundMessage()
    {
        var result = await _provider.CompleteAsync("system", "Contexto:\n\nPregunta: algo", new LlmOptions("rag-answer-v1.0"));

        Assert.Equal("Información no encontrada en las fuentes analizadas.", result.RawText);
    }
}
