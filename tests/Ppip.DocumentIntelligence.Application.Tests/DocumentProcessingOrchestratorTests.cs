using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Application.Tests.Fakes;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;
using Xunit;

namespace Ppip.DocumentIntelligence.Application.Tests;

public class DocumentProcessingOrchestratorTests
{
    private sealed class Harness
    {
        public InMemoryDocumentRepository Documents { get; } = new();
        public InMemoryObjectStorage Storage { get; } = new();
        public FakePdfExtractor PdfExtractor { get; } = new();
        public FakeOcrService OcrService { get; } = new();
        public InMemoryDocumentChunkRepository Chunks { get; } = new();
        public InMemoryOutboxStore Outbox { get; } = new();

        public DocumentProcessingOrchestrator Build()
        {
            var options = Options.Create(new DocumentProcessingOptions
            {
                TextualDensityThreshold = 0.01,
                ScannedDensityThreshold = 0.001,
                TargetChunkTokens = 200,
                MaxChunkTokens = 300,
                ChunkOverlapTokens = 10,
            });
            var publisher = new DocumentEventPublisher(Outbox);
            return new DocumentProcessingOrchestrator(Documents, Storage, PdfExtractor, OcrService, Chunks, publisher, options, NullLogger<DocumentProcessingOrchestrator>.Instance);
        }

        public async Task<Document> SeedDownloadedDocumentAsync()
        {
            var document = Document.Detect(DocumentId.New(), "418-1191-COT26", "https://docs.mercadopublico.cl/x.pdf", "bases.pdf", "corr-seed");
            var version = DocumentVersion.Create(Sha256Hash.From(new string('a', 64)), StorageRef.From("chilecompra", "418-1191-COT26/original/bases.pdf"), 2048);
            document.CompleteDownload(version, "corr-seed");
            await Documents.SaveAsync(document);
            return document;
        }
    }

    [Fact]
    public async Task ProcessAsync_TextualDocument_ExtractsAndChunksWithoutOcr()
    {
        var harness = new Harness();
        var document = await harness.SeedDownloadedDocumentAsync();
        harness.PdfExtractor.Result = new ExtractedPdf([
            new ExtractedPage(1, "1. Objeto\n\nSe requiere adquirir notebooks.", 0.02, HasTableLikeLayout: false, EmbeddedImages: []),
        ]);
        var orchestrator = harness.Build();

        var result = await orchestrator.ProcessAsync(document.Id, "corr-1");

        var version = result.CurrentVersion!;
        Assert.Equal(DocumentProcessingStage.Chunked, version.ProcessingStage);
        Assert.Equal(DocumentClass.Textual, version.Classification);
        Assert.Equal(0, harness.OcrService.CallCount);
        Assert.NotEmpty(harness.Chunks.Chunks);
        Assert.Contains(harness.Outbox.Messages, m => m.EventType == "DocumentExtracted");
        Assert.Contains(harness.Outbox.Messages, m => m.EventType == "DocumentChunked");
        Assert.DoesNotContain(harness.Outbox.Messages, m => m.EventType == "OcrCompleted");
    }

    [Fact]
    public async Task ProcessAsync_ScannedPageWithEmbeddedImage_RunsOcrAndPublishesOcrCompleted()
    {
        var harness = new Harness();
        var document = await harness.SeedDownloadedDocumentAsync();
        harness.PdfExtractor.Result = new ExtractedPdf([
            new ExtractedPage(1, string.Empty, 0.0001, HasTableLikeLayout: false, EmbeddedImages: [[1, 2, 3]]),
        ]);
        harness.OcrService.Text = "texto reconocido por ocr con suficientes palabras para un chunk";
        var orchestrator = harness.Build();

        var result = await orchestrator.ProcessAsync(document.Id, "corr-1");

        Assert.Equal(1, harness.OcrService.CallCount);
        Assert.Equal(DocumentClass.Scanned, result.CurrentVersion!.Classification);
        Assert.Contains(harness.Outbox.Messages, m => m.EventType == "OcrCompleted");
    }

    [Fact]
    public async Task ProcessAsync_LowDensityPageWithoutEmbeddedImage_SkipsOcrForThatPage()
    {
        var harness = new Harness();
        var document = await harness.SeedDownloadedDocumentAsync();
        harness.PdfExtractor.Result = new ExtractedPdf([
            new ExtractedPage(1, string.Empty, 0.0001, HasTableLikeLayout: false, EmbeddedImages: []),
        ]);
        var orchestrator = harness.Build();

        await orchestrator.ProcessAsync(document.Id, "corr-1");

        Assert.Equal(0, harness.OcrService.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_AlreadyChunked_ShortCircuits()
    {
        var harness = new Harness();
        var document = await harness.SeedDownloadedDocumentAsync();
        harness.PdfExtractor.Result = new ExtractedPdf([new ExtractedPage(1, "texto suficiente para un chunk de prueba", 0.02, false, [])]);
        var orchestrator = harness.Build();
        await orchestrator.ProcessAsync(document.Id, "corr-1");
        var extractCallsAfterFirst = harness.PdfExtractor.CallCount;

        var second = await orchestrator.ProcessAsync(document.Id, "corr-2");

        Assert.Equal(DocumentProcessingStage.Chunked, second.CurrentVersion!.ProcessingStage);
        Assert.Equal(extractCallsAfterFirst, harness.PdfExtractor.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_ExtractionThrows_MarksProcessingFailed()
    {
        var harness = new Harness();
        var document = await harness.SeedDownloadedDocumentAsync();
        harness.PdfExtractor.ThrowOnNextCall = new InvalidOperationException("PDF corrupto");
        var orchestrator = harness.Build();

        var result = await orchestrator.ProcessAsync(document.Id, "corr-1");

        Assert.Equal(DocumentProcessingStage.ProcessingFailed, result.CurrentVersion!.ProcessingStage);
        Assert.Equal("PDF corrupto", result.CurrentVersion.ProcessingFailureReason);
        Assert.Empty(harness.Chunks.Chunks);
    }

    [Fact]
    public async Task ProcessAsync_NoDownloadedVersion_Throws()
    {
        var harness = new Harness();
        var document = Document.Detect(DocumentId.New(), "418-1191-COT26", "https://docs.mercadopublico.cl/x.pdf", "bases.pdf", "corr-1");
        await harness.Documents.SaveAsync(document);
        var orchestrator = harness.Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => orchestrator.ProcessAsync(document.Id, "corr-2"));
    }
}
