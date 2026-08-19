using Ppip.BuildingBlocks.Messaging;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;
using Ppip.Knowledge.Domain;
using Ppip.Knowledge.Domain.Ports;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Domain.Ports;

namespace Ppip.Knowledge.Application.Tests.Fakes;

public sealed class FakeCompraAgilRepository : ICompraAgilRepository
{
    private readonly Dictionary<string, CompraAgil> _byId = [];

    public void Add(CompraAgil compra) => _byId[compra.Id.Value] = compra;

    public Task<CompraAgil?> FindAsync(CompraAgilId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.GetValueOrDefault(id.Value));

    public Task SaveAsync(CompraAgil compra, CancellationToken cancellationToken = default)
    {
        Add(compra);
        return Task.CompletedTask;
    }
}

public sealed class FakeDocumentRepository : IDocumentRepository
{
    private readonly Dictionary<Guid, Document> _byId = [];

    public void Add(Document document) => _byId[document.Id.Value] = document;

    public Task<Document?> FindAsync(DocumentId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.GetValueOrDefault(id.Value));

    public Task<Document?> FindByCompraAndUrlAsync(string compraAgilId, string sourceUrl, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(d => d.CompraAgilId == compraAgilId && d.SourceUrl == sourceUrl));

    public Task<IReadOnlyList<Document>> FindByCompraAsync(string compraAgilId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Document>>([.. _byId.Values.Where(d => d.CompraAgilId == compraAgilId)]);

    public Task SaveAsync(Document document, CancellationToken cancellationToken = default)
    {
        Add(document);
        return Task.CompletedTask;
    }
}

public sealed class FakeDocumentChunkRepository : IDocumentChunkRepository
{
    public List<DocumentChunk> Chunks { get; } = [];

    public Task SaveManyAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        Chunks.AddRange(chunks);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentChunk>> FindByVersionAsync(Guid versionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentChunk>>([.. Chunks.Where(c => c.VersionId == versionId).OrderBy(c => c.Page)]);

    public Task<IReadOnlyList<DocumentChunk>> FindByIdsAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DocumentChunk>>([.. Chunks.Where(c => chunkIds.Contains(c.Id))]);

    public Task MarkEmbeddedAsync(Guid chunkId, Guid embeddingId, CancellationToken cancellationToken = default)
    {
        Chunks.Single(c => c.Id == chunkId).MarkEmbedded(embeddingId);
        return Task.CompletedTask;
    }
}

public sealed class FakeEmbeddingProvider : IEmbeddingProvider
{
    public Exception? ThrowOnNextCall { get; set; }

    public Task<EmbeddingVector> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (ThrowOnNextCall is { } exception)
        {
            ThrowOnNextCall = null;
            throw exception;
        }

        return Task.FromResult(new EmbeddingVector([1f, 0f, 0f], "fake-embedding-v1", 3));
    }
}

public sealed class FakeVectorIndex : IVectorIndex
{
    public List<VectorPoint> Points { get; } = [];
    public Exception? ThrowOnNextSearch { get; set; }

    /// <summary>Resultados a devolver en el próximo SearchAsync — si es null, calcula desde <see cref="Points"/> con score fijo 0.9.</summary>
    public IReadOnlyList<VectorSearchResult>? NextSearchResults { get; set; }

    public Task UpsertAsync(VectorPoint point, CancellationToken cancellationToken = default)
    {
        Points.Add(point);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] queryVector, string compraAgilId, int topK, CancellationToken cancellationToken = default)
    {
        if (ThrowOnNextSearch is { } exception)
        {
            ThrowOnNextSearch = null;
            throw exception;
        }

        if (NextSearchResults is { } results)
        {
            return Task.FromResult(results);
        }

        var matches = Points
            .Where(p => p.Payload.CompraAgilId == compraAgilId)
            .Take(topK)
            .Select(p => new VectorSearchResult(p.PointId, 0.9f, p.Payload))
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(matches);
    }
}

public sealed class FakeLlmProvider : ILlmProvider
{
    public Exception? ThrowOnNextCall { get; set; }
    public Func<string, string>? ResponseFactory { get; set; }

    public Task<LlmCompletionResult> CompleteAsync(string systemPrompt, string userPrompt, LlmOptions options, CancellationToken cancellationToken = default)
    {
        if (ThrowOnNextCall is { } exception)
        {
            ThrowOnNextCall = null;
            throw exception;
        }

        var text = ResponseFactory?.Invoke(userPrompt) ?? "Respuesta de prueba [1].";
        return Task.FromResult(new LlmCompletionResult(text, "fake-llm-v1", 10, 5, 1));
    }
}

public sealed class FakeEmbeddingRepository : IEmbeddingRepository
{
    public List<Embedding> Embeddings { get; } = [];

    public Task SaveAsync(Embedding embedding, CancellationToken cancellationToken = default)
    {
        Embeddings.Add(embedding);
        return Task.CompletedTask;
    }
}

public sealed class FakeAIExecutionRepository : IAIExecutionRepository
{
    public List<AIExecution> Executions { get; } = [];

    public Task SaveAsync(AIExecution execution, CancellationToken cancellationToken = default)
    {
        Executions.Add(execution);
        return Task.CompletedTask;
    }
}

public sealed class FakeOutboxStore : IOutboxStore
{
    public List<OutboxMessage> Messages { get; } = [];

    public Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>([.. Messages]);

    public Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
