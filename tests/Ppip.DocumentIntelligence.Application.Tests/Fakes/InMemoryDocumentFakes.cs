using Ppip.BuildingBlocks.Messaging;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Application.Tests.Fakes;

public sealed class FakeAttachmentDownloader : IAttachmentDownloader
{
    public byte[] Content { get; set; } = "%PDF-1.7 contenido de prueba"u8.ToArray();
    public string? ContentType { get; set; } = "application/pdf";
    public Exception? ThrowOnNextCall { get; set; }
    public int CallCount { get; private set; }

    public Task<DownloadedAttachment> DownloadAsync(Uri url, long maxBytes, CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (ThrowOnNextCall is { } exception)
        {
            ThrowOnNextCall = null;
            throw exception;
        }

        return Task.FromResult(new DownloadedAttachment(Content, ContentType));
    }
}

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly Dictionary<Guid, Document> _byId = [];

    public int SaveCount { get; private set; }

    public Task<Document?> FindAsync(DocumentId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.GetValueOrDefault(id.Value));

    public Task<Document?> FindByCompraAndUrlAsync(string compraAgilId, string sourceUrl, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(d => d.CompraAgilId == compraAgilId && d.SourceUrl == sourceUrl));

    public Task<IReadOnlyList<Document>> FindByCompraAsync(string compraAgilId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Document>>([.. _byId.Values.Where(d => d.CompraAgilId == compraAgilId)]);

    public Task SaveAsync(Document document, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        _byId[document.Id.Value] = document;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly Dictionary<string, byte[]> _store = [];

    public int SaveCount { get; private set; }
    public byte[] ContentToLoad { get; set; } = "%PDF-1.7 contenido de prueba"u8.ToArray();

    public Task<StorageRef> SaveAsync(string bucket, string key, byte[] content, string? contentType, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        _store[$"{bucket}/{key}"] = content;
        return Task.FromResult(StorageRef.From(bucket, key));
    }

    public Task<byte[]> LoadAsync(StorageRef storageRef, CancellationToken cancellationToken = default)
    {
        var key = $"{storageRef.Bucket}/{storageRef.Key}";
        return Task.FromResult(_store.GetValueOrDefault(key, ContentToLoad));
    }
}

public sealed class FakePdfExtractor : IPdfExtractor
{
    public ExtractedPdf Result { get; set; } = new([new ExtractedPage(1, "texto nativo", 0.02, HasTableLikeLayout: false, EmbeddedImages: [])]);
    public Exception? ThrowOnNextCall { get; set; }
    public int CallCount { get; private set; }

    public ExtractedPdf Extract(byte[] pdfBytes)
    {
        CallCount++;
        if (ThrowOnNextCall is { } exception)
        {
            ThrowOnNextCall = null;
            throw exception;
        }

        return Result;
    }
}

public sealed class FakeOcrService : IOcrService
{
    public string Text { get; set; } = "[texto ocr simulado]";
    public double Confidence { get; set; } = 0.8;
    public int CallCount { get; private set; }

    public Task<OcrResult> RecognizeAsync(byte[] pageImage, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(new OcrResult(Text, Confidence));
    }
}

public sealed class InMemoryDocumentChunkRepository : IDocumentChunkRepository
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

public sealed class FakeMalwareScanner : IMalwareScanner
{
    public bool IsClean { get; set; } = true;

    public Task<ScanResult> ScanAsync(byte[] content, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ScanResult(IsClean, IsClean ? null : "eicar-test-signature"));
}

public sealed class InMemoryOutboxStore : IOutboxStore
{
    public List<OutboxMessage> Messages { get; } = [];

    public Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>([.. Messages.Where(m => !m.IsPublished).Take(maxCount)]);

    public Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        Messages.Single(m => m.Id == messageId).MarkPublished(publishedAt);
        return Task.CompletedTask;
    }
}
