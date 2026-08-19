using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Domain;
using Ppip.Knowledge.Application.Exceptions;
using Ppip.Knowledge.Application.Tests.Fakes;
using Ppip.Knowledge.Domain;
using Ppip.Knowledge.Domain.Exceptions;
using Ppip.Knowledge.Domain.Ports;
using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Knowledge.Application.Tests;

public class RagQueryOrchestratorTests
{
    private sealed class Harness
    {
        public FakeCompraAgilRepository Compras { get; } = new();
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentChunkRepository Chunks { get; } = new();
        public FakeEmbeddingProvider EmbeddingProvider { get; } = new();
        public FakeVectorIndex VectorIndex { get; } = new();
        public FakeLlmProvider LlmProvider { get; } = new();
        public FakeAIExecutionRepository Executions { get; } = new();

        public RagQueryOrchestrator Build(RagQueryOptions? options = null) =>
            new(Compras, Documents, Chunks, EmbeddingProvider, VectorIndex, LlmProvider, Executions, Options.Create(options ?? new RagQueryOptions()), NullLogger<RagQueryOrchestrator>.Instance);

        public (CompraAgil Compra, Document Document, DocumentChunk Chunk) SeedIndexedChunk(string compraAgilId = "418-1191-COT26")
        {
            var institucion = InstitutionRef.From("742", "Municipalidad de Ejemplo");
            var vigencia = DateRange.From(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5));
            var compra = CompraAgil.Detect(
                CompraAgilId.From(compraAgilId), institucion, "Compra de notebooks", Money.From(1_000_000, "CLP"), vigencia,
                "hash-1", [ProductRequirement.Create("Notebook", 10, "unidad")], "corr-seed");
            Compras.Add(compra);

            var document = Document.Detect(DocumentId.New(), compraAgilId, "https://docs.mercadopublico.cl/x.pdf", "Bases.pdf", "corr-seed");
            var version = DocumentVersion.Create(Sha256Hash.From(new string('a', 64)), StorageRef.From("chilecompra", "x/original/bases.pdf"), 2048);
            document.CompleteDownload(version, "corr-seed");
            Documents.Add(document);

            var chunk = DocumentChunk.Create(
                document.Id, version.Id, compraAgilId, 7, "1. Plazo de entrega", null, ChunkType.Paragraph,
                "El proveedor deberá entregar los productos en un plazo no superior a 10 días hábiles.", 12);
            Chunks.Chunks.Add(chunk);

            var payload = new VectorPayload(compraAgilId, document.Id.Value, version.Id, chunk.Page, chunk.Section, chunk.ChunkType.ToString(), "chilecompra", chunk.Hash, IsDemoData: false);
            VectorIndex.Points.Add(new VectorPoint(chunk.Id.ToString(), [1f, 0f, 0f], payload));

            return (compra, document, chunk);
        }
    }

    [Fact]
    public async Task QueryAsync_CompraDoesNotExist_ThrowsCompraNotFound()
    {
        var harness = new Harness();
        var orchestrator = harness.Build();

        await Assert.ThrowsAsync<CompraNotFoundException>(() =>
            orchestrator.QueryAsync(new RagQueryRequest("no-existe", "¿Cuál es el plazo de entrega?", 8), "corr-1"));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    public async Task QueryAsync_QuestionTooShort_ThrowsArgumentException(string question)
    {
        var harness = new Harness();
        harness.SeedIndexedChunk();
        var orchestrator = harness.Build();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", question, 8), "corr-1"));
    }

    [Fact]
    public async Task QueryAsync_TopKOutOfRange_ThrowsArgumentException()
    {
        var harness = new Harness();
        harness.SeedIndexedChunk();
        var orchestrator = harness.Build();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", "¿Cuál es el plazo de entrega?", 99), "corr-1"));
    }

    [Fact]
    public async Task QueryAsync_NoRelevantChunks_ReturnsUnansweredUnknown()
    {
        var harness = new Harness();
        harness.SeedIndexedChunk();
        harness.VectorIndex.NextSearchResults = [];
        var orchestrator = harness.Build();

        var answer = await orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", "¿Cuál es el plazo de entrega?", 8), "corr-1");

        Assert.True(answer.Unanswered);
        Assert.Equal(AnswerType.Unknown, answer.AnswerType);
        Assert.Empty(answer.Evidence);
        Assert.Equal("Información no encontrada en las fuentes analizadas.", answer.Answer);
        Assert.Single(harness.Executions.Executions);
    }

    [Fact]
    public async Task QueryAsync_RelevantChunkCitedByLlm_ReturnsFactWithEvidence()
    {
        var harness = new Harness();
        var (_, _, chunk) = harness.SeedIndexedChunk();
        var orchestrator = harness.Build();

        var answer = await orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", "¿Cuál es el plazo de entrega?", 8), "corr-1");

        Assert.False(answer.Unanswered);
        Assert.Equal(AnswerType.Fact, answer.AnswerType);
        Assert.Single(answer.Evidence);
        Assert.Equal(chunk.Id.ToString(), answer.Evidence[0].ChunkId);
        Assert.Equal(chunk.Text, answer.Evidence[0].SourceText);
        Assert.NotNull(answer.Execution);
        Assert.Single(harness.Executions.Executions);
    }

    [Fact]
    public async Task QueryAsync_LlmDoesNotCiteAnyChunk_DegradesToInference()
    {
        var harness = new Harness();
        harness.SeedIndexedChunk();
        harness.LlmProvider.ResponseFactory = _ => "Una respuesta sin ninguna cita.";
        var orchestrator = harness.Build();

        var answer = await orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", "¿Cuál es el plazo de entrega?", 8), "corr-1");

        Assert.Equal(AnswerType.Inference, answer.AnswerType);
        Assert.Empty(answer.Evidence);
        Assert.False(answer.Unanswered);
    }

    [Fact]
    public async Task QueryAsync_LlmUnavailable_DegradesToSearchOnlyResults_UC005_A3()
    {
        var harness = new Harness();
        var (_, _, chunk) = harness.SeedIndexedChunk();
        harness.LlmProvider.ThrowOnNextCall = new LlmUnavailableException("Ollama caído.");
        var orchestrator = harness.Build();

        var answer = await orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", "¿Cuál es el plazo de entrega?", 8), "corr-1");

        Assert.False(answer.Unanswered);
        Assert.Null(answer.Execution);
        Assert.Single(answer.Evidence);
        Assert.Equal(chunk.Id.ToString(), answer.Evidence[0].ChunkId);
    }

    [Fact]
    public async Task QueryAsync_VectorIndexDown_ThrowsRetrievalUnavailable_UC005_A2()
    {
        var harness = new Harness();
        harness.SeedIndexedChunk();
        harness.VectorIndex.ThrowOnNextSearch = new HttpRequestException("Qdrant caído.");
        var orchestrator = harness.Build();

        await Assert.ThrowsAsync<RetrievalUnavailableException>(() =>
            orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", "¿Cuál es el plazo de entrega?", 8), "corr-1"));
    }

    [Fact]
    public async Task QueryAsync_EmbeddingProviderDown_ThrowsRetrievalUnavailable_UC005_A2()
    {
        var harness = new Harness();
        harness.SeedIndexedChunk();
        harness.EmbeddingProvider.ThrowOnNextCall = new HttpRequestException("Ollama caído.");
        var orchestrator = harness.Build();

        await Assert.ThrowsAsync<RetrievalUnavailableException>(() =>
            orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", "¿Cuál es el plazo de entrega?", 8), "corr-1"));
    }

    [Fact]
    public async Task QueryAsync_NeverMutatesDomainState()
    {
        // Postcondición UC-005: "Ninguna mutación de dominio" — solo se persiste auditoría (AIExecution).
        var harness = new Harness();
        harness.SeedIndexedChunk();
        var orchestrator = harness.Build();

        await orchestrator.QueryAsync(new RagQueryRequest("418-1191-COT26", "¿Cuál es el plazo de entrega?", 8), "corr-1");

        Assert.Single(harness.Executions.Executions);
        Assert.Single(harness.VectorIndex.Points);
        Assert.Single(harness.Chunks.Chunks);
        Assert.All(harness.Chunks.Chunks, c => Assert.Null(c.EmbeddingId));
    }
}
