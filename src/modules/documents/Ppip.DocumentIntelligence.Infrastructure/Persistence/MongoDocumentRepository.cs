using MongoDB.Driver;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Persistence;

public sealed class MongoDocumentRepository : IDocumentRepository
{
    private readonly IMongoCollection<DocumentRecord> _documents;
    private readonly IMongoCollection<DocumentVersionRecord> _versions;
    private readonly IMongoCollection<DocumentPageRecord> _pages;

    public MongoDocumentRepository(IMongoDatabase database)
    {
        _documents = database.GetCollection<DocumentRecord>("documents");
        _versions = database.GetCollection<DocumentVersionRecord>("document_versions");
        _pages = database.GetCollection<DocumentPageRecord>("document_pages");
    }

    public async Task<Document?> FindAsync(DocumentId id, CancellationToken cancellationToken = default)
    {
        var record = await _documents.Find(d => d.Id == id.Value).FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : await ToDomainAsync(record, cancellationToken);
    }

    public async Task<Document?> FindByCompraAndUrlAsync(string compraAgilId, string sourceUrl, CancellationToken cancellationToken = default)
    {
        var record = await _documents.Find(d => d.CompraAgilId == compraAgilId && d.SourceUrl == sourceUrl).FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : await ToDomainAsync(record, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> FindByCompraAsync(string compraAgilId, CancellationToken cancellationToken = default)
    {
        var records = await _documents.Find(d => d.CompraAgilId == compraAgilId).ToListAsync(cancellationToken);
        var result = new List<Document>();
        foreach (var record in records)
        {
            result.Add(await ToDomainAsync(record, cancellationToken));
        }

        return result;
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

        // A diferencia de FASE 7 (solo insertaba versiones nuevas por hash),
        // ahora se hace upsert por Id de TODAS las versiones: el binario es
        // inmutable, pero los campos de procesamiento (FASE 8) se actualizan
        // sobre la misma versión a medida que avanza el pipeline.
        foreach (var version in document.Versions)
        {
            var versionRecord = new DocumentVersionRecord
            {
                Id = version.Id,
                DocumentId = id,
                Sha256 = version.Sha256Hash.Value,
                Bucket = version.StorageRef.Bucket,
                Key = version.StorageRef.Key,
                SizeBytes = version.SizeBytes,
                DownloadedAt = version.DownloadedAt,
                ProcessingStage = version.ProcessingStage.ToString(),
                Classification = version.Classification?.ToString(),
                ProcessingFailureReason = version.ProcessingFailureReason,
            };

            await _versions.ReplaceOneAsync(v => v.Id == version.Id, versionRecord, new ReplaceOptions { IsUpsert = true }, cancellationToken);

            if (version.Pages.Count > 0)
            {
                await SavePagesAsync(version, cancellationToken);
            }
        }
    }

    private async Task SavePagesAsync(DocumentVersion version, CancellationToken cancellationToken)
    {
        // Las páginas se reemplazan como conjunto en cada extracción/OCR (no
        // son append-only como las versiones): borra lo anterior de esta
        // versión y reinserta el estado actual completo.
        await _pages.DeleteManyAsync(p => p.VersionId == version.Id, cancellationToken);

        var records = version.Pages.Select(p => new DocumentPageRecord
        {
            Id = p.Id,
            VersionId = version.Id,
            PageNumber = p.PageNumber,
            Text = p.Text,
            ExtractionMethod = p.ExtractionMethod.ToString(),
            TextDensity = p.TextDensity,
            OcrConfidence = p.OcrConfidence,
        }).ToList();

        if (records.Count > 0)
        {
            await _pages.InsertManyAsync(records, cancellationToken: cancellationToken);
        }
    }

    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var versions = database.GetCollection<DocumentVersionRecord>("document_versions");
        var keys = Builders<DocumentVersionRecord>.IndexKeys.Ascending(v => v.DocumentId).Ascending(v => v.Sha256);
        await versions.Indexes.CreateOneAsync(
            new CreateIndexModel<DocumentVersionRecord>(keys, new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        var pages = database.GetCollection<DocumentPageRecord>("document_pages");
        var pageKeys = Builders<DocumentPageRecord>.IndexKeys.Ascending(p => p.VersionId).Ascending(p => p.PageNumber);
        await pages.Indexes.CreateOneAsync(new CreateIndexModel<DocumentPageRecord>(pageKeys), cancellationToken: cancellationToken);
    }

    private async Task<Document> ToDomainAsync(DocumentRecord record, CancellationToken cancellationToken)
    {
        var versionRecords = await _versions.Find(v => v.DocumentId == record.Id).SortBy(v => v.DownloadedAt).ToListAsync(cancellationToken);
        var versions = new List<DocumentVersion>();
        foreach (var versionRecord in versionRecords)
        {
            var pageRecords = await _pages.Find(p => p.VersionId == versionRecord.Id).SortBy(p => p.PageNumber).ToListAsync(cancellationToken);
            var pages = pageRecords.Select(p => DocumentPage.Rehydrate(
                p.Id,
                p.PageNumber,
                p.Text,
                Enum.Parse<ExtractionMethod>(p.ExtractionMethod),
                p.TextDensity,
                p.OcrConfidence));

            versions.Add(DocumentVersion.Rehydrate(
                versionRecord.Id,
                Sha256Hash.From(versionRecord.Sha256),
                StorageRef.From(versionRecord.Bucket, versionRecord.Key),
                versionRecord.SizeBytes,
                versionRecord.DownloadedAt,
                Enum.Parse<DocumentProcessingStage>(versionRecord.ProcessingStage),
                versionRecord.Classification is null ? null : Enum.Parse<DocumentClass>(versionRecord.Classification),
                versionRecord.ProcessingFailureReason,
                pages));
        }

        return Document.Rehydrate(
            DocumentId.From(record.Id),
            record.CompraAgilId,
            record.SourceUrl,
            record.DeclaredName,
            Enum.Parse<DocumentStage>(record.Stage),
            record.FailureReason,
            versions);
    }
}
