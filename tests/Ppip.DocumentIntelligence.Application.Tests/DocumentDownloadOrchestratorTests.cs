using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ppip.DocumentIntelligence.Application.Tests.Fakes;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Exceptions;
using Xunit;

namespace Ppip.DocumentIntelligence.Application.Tests;

public class DocumentDownloadOrchestratorTests
{
    private const string CompraAgilId = "418-1191-COT26";
    private const string AllowedUrl = "https://docs.mercadopublico.cl/bases.pdf";
    private const string DisallowedUrl = "https://attacker.example.com/bases.pdf";

    private sealed class Harness
    {
        public FakeAttachmentDownloader Downloader { get; } = new();
        public InMemoryDocumentRepository Documents { get; } = new();
        public InMemoryObjectStorage Storage { get; } = new();
        public FakeMalwareScanner Scanner { get; } = new();
        public InMemoryOutboxStore Outbox { get; } = new();

        public DocumentDownloadOrchestrator Build()
        {
            var options = Options.Create(new DocumentDownloadOptions());
            var publisher = new DocumentEventPublisher(Outbox);
            return new DocumentDownloadOrchestrator(
                Documents, Storage, Downloader, Scanner, publisher, options, NullLogger<DocumentDownloadOrchestrator>.Instance);
        }
    }

    [Fact]
    public async Task HappyPath_DownloadsStoresAndPublishesBothEvents()
    {
        var harness = new Harness();
        var orchestrator = harness.Build();

        var document = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-1");

        Assert.Equal(DocumentStage.Downloaded, document.Stage);
        Assert.Single(document.Versions);
        Assert.Equal(1, harness.Storage.SaveCount);
        Assert.Equal(2, harness.Outbox.Messages.Count); // DocumentDetected + DocumentDownloaded
        Assert.Contains(harness.Outbox.Messages, m => m.EventType == "DocumentDetected");
        Assert.Contains(harness.Outbox.Messages, m => m.EventType == "DocumentDownloaded");
    }

    [Fact]
    public async Task DisallowedUrl_RejectedByPolicy_NeverAttemptsDownload()
    {
        var harness = new Harness();
        var orchestrator = harness.Build();

        var document = await orchestrator.ProcessAsync(CompraAgilId, DisallowedUrl, "bases.pdf", "corr-1");

        Assert.Equal(DocumentStage.RejectedByPolicy, document.Stage);
        Assert.Equal(0, harness.Downloader.CallCount);
        Assert.Equal(0, harness.Storage.SaveCount);
        Assert.Single(harness.Outbox.Messages); // solo DocumentDetected, nunca Downloaded
    }

    [Fact]
    public async Task OversizedAttachment_RejectedByPolicy()
    {
        var harness = new Harness();
        harness.Downloader.ThrowOnNextCall = new AttachmentTooLargeException("supera el máximo");
        var orchestrator = harness.Build();

        var document = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-1");

        Assert.Equal(DocumentStage.RejectedByPolicy, document.Stage);
        Assert.Equal(0, harness.Storage.SaveCount);
    }

    [Fact]
    public async Task SsrfBlockedAtConnectTime_RejectedByPolicy()
    {
        var harness = new Harness();
        harness.Downloader.ThrowOnNextCall = new AttachmentBlockedException("IP privada");
        var orchestrator = harness.Build();

        var document = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-1");

        Assert.Equal(DocumentStage.RejectedByPolicy, document.Stage);
    }

    [Fact]
    public async Task WrongContentType_RejectedByPolicy()
    {
        var harness = new Harness();
        harness.Downloader.ContentType = "text/html";
        var orchestrator = harness.Build();

        var document = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-1");

        Assert.Equal(DocumentStage.RejectedByPolicy, document.Stage);
        Assert.Equal(0, harness.Storage.SaveCount);
    }

    [Fact]
    public async Task ContentTypeLiesAboutBeingPdf_MagicBytesMismatch_RejectedByPolicy()
    {
        var harness = new Harness();
        harness.Downloader.Content = "<!DOCTYPE html><html>no es un pdf</html>"u8.ToArray();
        harness.Downloader.ContentType = "application/pdf";
        var orchestrator = harness.Build();

        var document = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-1");

        Assert.Equal(DocumentStage.RejectedByPolicy, document.Stage);
        Assert.Equal(0, harness.Storage.SaveCount);
    }

    [Fact]
    public async Task MalwareDetected_RejectedByPolicy()
    {
        var harness = new Harness();
        harness.Scanner.IsClean = false;
        var orchestrator = harness.Build();

        var document = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-1");

        Assert.Equal(DocumentStage.RejectedByPolicy, document.Stage);
        Assert.Contains("eicar", document.FailureReason);
        Assert.Equal(0, harness.Storage.SaveCount);
    }

    [Fact]
    public async Task NetworkFailure_MarksDownloadFailed_ThenRetrySucceeds()
    {
        var harness = new Harness();
        harness.Downloader.ThrowOnNextCall = new HttpRequestException("simulated 500 after retries exhausted");
        var orchestrator = harness.Build();

        var failed = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-1");
        Assert.Equal(DocumentStage.DownloadFailed, failed.Stage);

        var retried = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-2");

        Assert.Equal(DocumentStage.Downloaded, retried.Stage);
        Assert.Equal(1, harness.Outbox.Messages.Count(m => m.EventType == "DocumentDetected")); // no se re-publica al reintentar
    }

    [Fact]
    public async Task AlreadyDownloaded_ShortCircuits_NeverReDownloads()
    {
        var harness = new Harness();
        var orchestrator = harness.Build();
        await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-1");
        var callsAfterFirst = harness.Downloader.CallCount;

        var second = await orchestrator.ProcessAsync(CompraAgilId, AllowedUrl, "bases.pdf", "corr-2");

        Assert.Equal(DocumentStage.Downloaded, second.Stage);
        Assert.Equal(callsAfterFirst, harness.Downloader.CallCount); // no reintentó la descarga
    }

    // Nota: el no-op por mismo hash (NFR-002) que Document.CompleteDownload
    // implementa se prueba directamente en Ppip.DocumentIntelligence.Domain.Tests
    // (CompleteDownload_SameHashAsCurrent_IsNoOp) — a través del orquestador
    // esa rama es inalcanzable a propósito: un documento en Downloaded nunca
    // vuelve a intentar descargar (ver AlreadyDownloaded_ShortCircuits_NeverReDownloads),
    // así que CompleteDownload jamás se llama dos veces sobre el mismo Document.
}
