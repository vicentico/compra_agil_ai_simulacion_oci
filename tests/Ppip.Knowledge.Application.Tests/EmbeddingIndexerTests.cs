using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Domain;
using Ppip.Knowledge.Application.Tests.Fakes;
using Ppip.Knowledge.Domain.Exceptions;
using Xunit;

namespace Ppip.Knowledge.Application.Tests;

public class EmbeddingIndexerTests
{
    private sealed class Harness
    {
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentChunkRepository Chunks { get; } = new();
        public FakeEmbeddingRepository Embeddings { get; } = new();
        public FakeEmbeddingProvider EmbeddingProvider { get; } = new();
        public FakeVectorIndex VectorIndex { get; } = new();
        public FakeOutboxStore Outbox { get; } = new();

        public EmbeddingIndexer Build()
        {
            var options = Options.Create(new EmbeddingIndexingOptions { Source = "chilecompra", Producer = "test@1.0.0" });
            var publisher = new KnowledgeEventPublisher(Outbox);
            return new EmbeddingIndexer(Documents, Chunks, Embeddings, EmbeddingProvider, VectorIndex, publisher, options, NullLogger<EmbeddingIndexer>.Instance);
        }

        public Document SeedChunkedDocument(string compraAgilId = "418-1191-COT26", int chunkCount = 2)
        {
            var document = Document.Detect(DocumentId.New(), compraAgilId, "https://docs.mercadopublico.cl/x.pdf", "bases.pdf", "corr-seed");
            var version = DocumentVersion.Create(Sha256Hash.From(new string('a', 64)), StorageRef.From("chilecompra", "x/original/bases.pdf"), 2048);
            document.CompleteDownload(version, "corr-seed");

            var page = DocumentPage.FromNativeText(1, "1. Objeto\n\nSe requiere adquirir notebooks.", 0.02);
            document.CompleteExtraction(DocumentClass.Textual, [page], "corr-seed");

            var chunks = Enumerable.Range(0, chunkCount)
                .Select(i => DocumentChunk.Create(document.Id, version.Id, compraAgilId, 1, "1. Objeto", null, ChunkType.Paragraph, $"texto {i}", 5))
                .ToList();
            Chunks.Chunks.AddRange(chunks);
            document.CompleteChunking(chunks, "corr-seed");

            Documents.Add(document);
            return document;
        }
    }

    [Fact]
    public async Task IndexAsync_ChunkedDocument_EmbedsAllPendingChunks()
    {
        var harness = new Harness();
        var document = harness.SeedChunkedDocument(chunkCount: 3);
        var indexer = harness.Build();

        var indexedCount = await indexer.IndexAsync(document.Id, "corr-1");

        Assert.Equal(3, indexedCount);
        Assert.Equal(3, harness.VectorIndex.Points.Count);
        Assert.Equal(3, harness.Embeddings.Embeddings.Count);
        Assert.All(harness.Chunks.Chunks, c => Assert.NotNull(c.EmbeddingId));
    }

    [Fact]
    public async Task IndexAsync_AlreadyEmbedded_IsIdempotent()
    {
        var harness = new Harness();
        var document = harness.SeedChunkedDocument(chunkCount: 2);
        var indexer = harness.Build();
        await indexer.IndexAsync(document.Id, "corr-1");

        var secondRunCount = await indexer.IndexAsync(document.Id, "corr-2");

        Assert.Equal(0, secondRunCount);
        Assert.Equal(2, harness.VectorIndex.Points.Count);
    }

    [Fact]
    public async Task IndexAsync_NotYetChunked_Throws()
    {
        var harness = new Harness();
        var document = Document.Detect(DocumentId.New(), "418-1191-COT26", "https://docs.mercadopublico.cl/x.pdf", "bases.pdf", "corr-seed");
        var version = DocumentVersion.Create(Sha256Hash.From(new string('a', 64)), StorageRef.From("chilecompra", "x/original/bases.pdf"), 2048);
        document.CompleteDownload(version, "corr-seed");
        harness.Documents.Add(document);
        var indexer = harness.Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => indexer.IndexAsync(document.Id, "corr-1"));
    }

    [Fact]
    public async Task IndexAsync_EmbeddingProviderDown_PropagatesRetrievalUnavailable()
    {
        var harness = new Harness();
        var document = harness.SeedChunkedDocument(chunkCount: 1);
        harness.EmbeddingProvider.ThrowOnNextCall = new RetrievalUnavailableException("Ollama caído.");
        var indexer = harness.Build();

        await Assert.ThrowsAsync<RetrievalUnavailableException>(() => indexer.IndexAsync(document.Id, "corr-1"));
    }

    [Fact]
    public async Task IndexAsync_SingleDocumentCompra_PublishesIsLastOfCompraTrue()
    {
        var harness = new Harness();
        var document = harness.SeedChunkedDocument(chunkCount: 1);
        var indexer = harness.Build();

        await indexer.IndexAsync(document.Id, "corr-1");

        Assert.Single(harness.Outbox.Messages);
        Assert.Contains("\"isLastOfCompra\":true", harness.Outbox.Messages[0].PayloadJson);
    }

    [Fact]
    public async Task IndexAsync_SiblingDocumentNotYetChunked_PublishesIsLastOfCompraFalse()
    {
        var harness = new Harness();
        var document = harness.SeedChunkedDocument(compraAgilId: "418-1191-COT26", chunkCount: 1);
        var sibling = Document.Detect(DocumentId.New(), "418-1191-COT26", "https://docs.mercadopublico.cl/anexo.pdf", "anexo.pdf", "corr-seed");
        var siblingVersion = DocumentVersion.Create(Sha256Hash.From(new string('b', 64)), StorageRef.From("chilecompra", "x/original/anexo.pdf"), 1024);
        sibling.CompleteDownload(siblingVersion, "corr-seed");
        harness.Documents.Add(sibling);
        var indexer = harness.Build();

        await indexer.IndexAsync(document.Id, "corr-1");

        Assert.Contains("\"isLastOfCompra\":false", harness.Outbox.Messages[0].PayloadJson);
    }
}
