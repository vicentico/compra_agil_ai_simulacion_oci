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

    public Task SaveAsync(Document document, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        _byId[document.Id.Value] = document;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryObjectStorage : IObjectStorage
{
    public int SaveCount { get; private set; }

    public Task<StorageRef> SaveAsync(string bucket, string key, byte[] content, string? contentType, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(StorageRef.From(bucket, key));
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
