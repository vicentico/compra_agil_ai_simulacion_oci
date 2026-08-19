using Ppip.Knowledge.Domain;
using Xunit;

namespace Ppip.Knowledge.Domain.Tests;

public class AIExecutionTests
{
    [Fact]
    public void Record_Valid_SetsFields()
    {
        var correlationId = Guid.CreateVersion7();

        var execution = AIExecution.Record("418-1191-COT26", "llama3.1:8b", "rag-answer-v1.0", 2314, 118, 2140, correlationId);

        Assert.Equal("418-1191-COT26", execution.CompraAgilId);
        Assert.Equal("llama3.1:8b", execution.Model);
        Assert.Equal("rag-answer-v1.0", execution.PromptVersion);
        Assert.Equal(2314, execution.TokensIn);
        Assert.Equal(118, execution.TokensOut);
        Assert.Equal(2140, execution.DurationMs);
        Assert.Equal(correlationId, execution.CorrelationId);
    }

    [Fact]
    public void Record_EmptyCompraAgilId_Throws()
    {
        Assert.Throws<ArgumentException>(() => AIExecution.Record("  ", "llama3.1:8b", "rag-answer-v1.0", 0, 0, 0, Guid.CreateVersion7()));
    }

    [Fact]
    public void Record_EmptyModel_Throws()
    {
        Assert.Throws<ArgumentException>(() => AIExecution.Record("418-1191-COT26", "  ", "rag-answer-v1.0", 0, 0, 0, Guid.CreateVersion7()));
    }

    [Fact]
    public void Record_EmptyPromptVersion_Throws()
    {
        Assert.Throws<ArgumentException>(() => AIExecution.Record("418-1191-COT26", "llama3.1:8b", "  ", 0, 0, 0, Guid.CreateVersion7()));
    }

    [Fact]
    public void Record_AllowsZeroTokens_ForUnansweredQueries()
    {
        // UC-005 A1: se audita la ejecución aunque no haya evidencia (0 tokens, sin llamada al LLM).
        var execution = AIExecution.Record("418-1191-COT26", "n/a", "rag-answer-v1.0", 0, 0, 0, Guid.CreateVersion7());

        Assert.Equal(0, execution.TokensIn);
        Assert.Equal(0, execution.TokensOut);
    }
}
