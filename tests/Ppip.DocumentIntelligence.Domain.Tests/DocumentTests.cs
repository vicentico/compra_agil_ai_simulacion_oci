using Ppip.DocumentIntelligence.Domain;
using Xunit;

namespace Ppip.DocumentIntelligence.Domain.Tests;

public class DocumentTests
{
    private static DocumentVersion NewVersion(char hashChar = 'a') =>
        DocumentVersion.Create(Sha256Hash.From(new string(hashChar, 64)), StorageRef.From("chilecompra", "418-1191-COT26/original/x.pdf"), sizeBytes: 1024);

    private static Document DetectDefault(string correlationId = "corr-1") =>
        Document.Detect(DocumentId.New(), "418-1191-COT26", "https://docs.mercadopublico.cl/x.pdf", "bases.pdf", correlationId);

    [Fact]
    public void Detect_StartsAtDetected_RaisesEvent()
    {
        var document = DetectDefault();

        Assert.Equal(DocumentStage.Detected, document.Stage);
        var evento = Assert.Single(document.DomainEvents);
        var detected = Assert.IsType<DocumentDetected>(evento);
        Assert.Equal(document.Id.ToString(), detected.DocumentId);
        Assert.Equal("418-1191-COT26", detected.CompraAgilId);
    }

    [Fact]
    public void RejectByPolicy_FromDetected_Succeeds()
    {
        var document = DetectDefault();

        document.RejectByPolicy("URL fuera de allowlist");

        Assert.Equal(DocumentStage.RejectedByPolicy, document.Stage);
        Assert.Equal("URL fuera de allowlist", document.FailureReason);
    }

    [Fact]
    public void RejectByPolicy_AfterDownloaded_Throws()
    {
        var document = DetectDefault();
        document.CompleteDownload(NewVersion(), "corr-2");

        Assert.Throws<InvalidOperationException>(() => document.RejectByPolicy("tarde"));
    }

    [Fact]
    public void MarkDownloadFailed_ThenRetrySucceeds()
    {
        var document = DetectDefault();

        document.MarkDownloadFailed("timeout");
        Assert.Equal(DocumentStage.DownloadFailed, document.Stage);

        var created = document.CompleteDownload(NewVersion(), "corr-2");

        Assert.True(created);
        Assert.Equal(DocumentStage.Downloaded, document.Stage);
        Assert.Null(document.FailureReason);
    }

    [Fact]
    public void CompleteDownload_FirstVersion_RaisesEventAndReturnsTrue()
    {
        var document = DetectDefault();
        document.PullDomainEvents();

        var created = document.CompleteDownload(NewVersion(), "corr-2");

        Assert.True(created);
        var evento = Assert.Single(document.DomainEvents);
        var downloaded = Assert.IsType<DocumentDownloaded>(evento);
        Assert.Equal(1024, downloaded.SizeBytes);
        Assert.Single(document.Versions);
    }

    [Fact]
    public void CompleteDownload_SameHashAsCurrent_IsNoOp()
    {
        var document = DetectDefault();
        document.CompleteDownload(NewVersion('b'), "corr-2");
        document.PullDomainEvents();

        var created = document.CompleteDownload(NewVersion('b'), "corr-3");

        Assert.False(created);
        Assert.Empty(document.DomainEvents);
        Assert.Single(document.Versions);
    }

    [Fact]
    public void CompleteDownload_DifferentHash_AddsNewVersion()
    {
        var document = DetectDefault();
        document.CompleteDownload(NewVersion('c'), "corr-2");
        document.PullDomainEvents();

        var created = document.CompleteDownload(NewVersion('d'), "corr-3");

        Assert.True(created);
        Assert.Equal(2, document.Versions.Count);
    }

    [Fact]
    public void Rehydrate_DoesNotRaiseEvents()
    {
        var document = Document.Rehydrate(
            DocumentId.New(), "418-1191-COT26", "https://docs.mercadopublico.cl/x.pdf", "bases.pdf",
            DocumentStage.Downloaded, failureReason: null, versions: [NewVersion()]);

        Assert.Empty(document.DomainEvents);
        Assert.Equal(DocumentStage.Downloaded, document.Stage);
        Assert.Single(document.Versions);
    }
}
