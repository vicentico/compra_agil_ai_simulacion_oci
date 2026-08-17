using MongoDB.Driver;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Infrastructure.Persistence;
using Testcontainers.MongoDb;
using Xunit;

namespace Ppip.DocumentIntelligence.Infrastructure.Tests.Persistence;

/// <summary>Contra un MongoDB real — valida el round-trip a través de dos colecciones (`documents` + `document_versions`, docs/08-data/01) y el índice único.</summary>
public sealed class MongoDocumentRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private MongoDocumentRepository _repository = null!;
    private IMongoDatabase _database = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        _database = client.GetDatabase("documents_test");
        _repository = new MongoDocumentRepository(_database);
        await MongoDocumentRepository.EnsureIndexesAsync(_database);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private static Document NewDetected() =>
        Document.Detect(DocumentId.New(), "418-1191-COT26", "https://docs.mercadopublico.cl/bases.pdf", "bases.pdf", "corr-1");

    [Fact]
    public async Task SaveThenFind_RoundTripsWithVersion()
    {
        var document = NewDetected();
        var version = DocumentVersion.Create(Sha256Hash.From(new string('a', 64)), StorageRef.From("chilecompra", "418-1191-COT26/original/bases.pdf"), sizeBytes: 2048);
        document.CompleteDownload(version, "corr-1");

        await _repository.SaveAsync(document);
        var found = await _repository.FindAsync(document.Id);

        Assert.NotNull(found);
        Assert.Equal(DocumentStage.Downloaded, found!.Stage);
        Assert.Single(found.Versions);
        Assert.Equal(version.Sha256Hash, found.Versions[0].Sha256Hash);
        Assert.Equal(2048, found.Versions[0].SizeBytes);
        Assert.Empty(found.DomainEvents);
    }

    [Fact]
    public async Task FindByCompraAndUrl_ReturnsSameIdentity()
    {
        var document = NewDetected();
        await _repository.SaveAsync(document);

        var found = await _repository.FindByCompraAndUrlAsync("418-1191-COT26", "https://docs.mercadopublico.cl/bases.pdf");

        Assert.NotNull(found);
        Assert.Equal(document.Id, found!.Id);
    }

    [Fact]
    public async Task Find_UnknownId_ReturnsNull()
    {
        var found = await _repository.FindAsync(DocumentId.New());

        Assert.Null(found);
    }

    [Fact]
    public async Task SaveTwice_AppendsOnlyNewVersions_NeverDuplicatesExisting()
    {
        var document = NewDetected();
        var v1 = DocumentVersion.Create(Sha256Hash.From(new string('a', 64)), StorageRef.From("chilecompra", "x/v1.pdf"), 100);
        document.CompleteDownload(v1, "corr-1");
        await _repository.SaveAsync(document);

        // Segundo guardado con el mismo estado — no debe duplicar la versión ya persistida.
        await _repository.SaveAsync(document);

        var found = await _repository.FindAsync(document.Id);
        Assert.Single(found!.Versions);
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateDocumentIdAndHash()
    {
        var documentId = Guid.NewGuid();
        var versions = _database.GetCollection<IndexProbeRecord>("document_versions");
        var record = new IndexProbeRecord
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sha256 = new string('a', 64),
        };
        await versions.InsertOneAsync(record);

        var duplicate = new IndexProbeRecord { Id = Guid.NewGuid(), DocumentId = documentId, Sha256 = record.Sha256 };
        await Assert.ThrowsAsync<MongoWriteException>(() => versions.InsertOneAsync(duplicate));
    }

    /// <summary>Solo los campos que importan para probar el índice único `{DocumentId, Sha256}` — misma colección física que <c>DocumentVersionRecord</c> (interno a Infrastructure).</summary>
    private sealed class IndexProbeRecord
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        [MongoDB.Bson.Serialization.Attributes.BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
        public Guid Id { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
        public Guid DocumentId { get; set; }

        public string Sha256 { get; set; } = string.Empty;
    }
}
