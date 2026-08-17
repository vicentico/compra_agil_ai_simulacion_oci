using MongoDB.Driver;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Persistence;

public sealed class MongoDocumentRepository : IDocumentRepository
{
    private readonly IMongoCollection<DocumentRecord> _documents;
    private readonly IMongoCollection<DocumentVersionRecord> _versions;

    public MongoDocumentRepository(IMongoDatabase database)
    {
        _documents = database.GetCollection<DocumentRecord>("documents");
        _versions = database.GetCollection<DocumentVersionRecord>("document_versions");
    }

    public async Task<Document?> FindAsync(DocumentId id, CancellationToken cancellationToken = default)
    {
        var record = await _documents.Find(d => d.Id == id.Value).FirstOrDefaultAsync(cancellationToken);
        if (record is null)
        {
            return null;
        }

        var versions = await _versions.Find(v => v.DocumentId == record.Id).SortBy(v => v.DownloadedAt).ToListAsync(cancellationToken);
        return ToDomain(record, versions);
    }

    public async Task<Document?> FindByCompraAndUrlAsync(string compraAgilId, string sourceUrl, CancellationToken cancellationToken = default)
    {
        var record = await _documents.Find(d => d.CompraAgilId == compraAgilId && d.SourceUrl == sourceUrl).FirstOrDefaultAsync(cancellationToken);
        if (record is null)
        {
            return null;
        }

        var versions = await _versions.Find(v => v.DocumentId == record.Id).SortBy(v => v.DownloadedAt).ToListAsync(cancellationToken);
        return ToDomain(record, versions);
    }

    public async Task SaveAsync(Document document, CancellationToken cancellationToken = default)
    {
        var id = document.Id.Value;
        var record = new DocumentRecord
        {
            Id = id,
            CompraAgilId = document.CompraAgilId,
            SourceUrl = document.SourceUrl,
            DeclaredName = document.DeclaredName,
            Stage = document.Stage.ToString(),
            FailureReason = document.FailureReason,
        };

        await _documents.ReplaceOneAsync(d => d.Id == id, record, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        // Versiones son append-only (docs/03-domain: "una versión nunca se
        // modifica") — solo se insertan las que todavía no existen por hash,
        // nunca se actualizan las ya guardadas.
        var existingHashes = (await _versions.Find(v => v.DocumentId == id)
            .Project(v => v.Sha256)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var newVersions = document.Versions
            .Where(v => !existingHashes.Contains(v.Sha256Hash.Value))
            .Select(v => new DocumentVersionRecord
            {
                Id = v.Id,
                DocumentId = id,
                Sha256 = v.Sha256Hash.Value,
                Bucket = v.StorageRef.Bucket,
                Key = v.StorageRef.Key,
                SizeBytes = v.SizeBytes,
                DownloadedAt = v.DownloadedAt,
            })
            .ToList();

        if (newVersions.Count > 0)
        {
            await _versions.InsertManyAsync(newVersions, cancellationToken: cancellationToken);
        }
    }

    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var versions = database.GetCollection<DocumentVersionRecord>("document_versions");
        var keys = Builders<DocumentVersionRecord>.IndexKeys.Ascending(v => v.DocumentId).Ascending(v => v.Sha256);
        await versions.Indexes.CreateOneAsync(
            new CreateIndexModel<DocumentVersionRecord>(keys, new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
    }

    private static Document ToDomain(DocumentRecord record, IEnumerable<DocumentVersionRecord> versions) =>
        Document.Rehydrate(
            DocumentId.From(record.Id),
            record.CompraAgilId,
            record.SourceUrl,
            record.DeclaredName,
            Enum.Parse<DocumentStage>(record.Stage),
            record.FailureReason,
            versions.Select(v => DocumentVersion.Rehydrate(v.Id, Sha256Hash.From(v.Sha256), StorageRef.From(v.Bucket, v.Key), v.SizeBytes, v.DownloadedAt)));
}
