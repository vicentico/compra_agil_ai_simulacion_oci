using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Domain.Ports;

namespace Ppip.Procurement.Infrastructure.Persistence;

public sealed class MongoInstitutionRepository : IInstitutionRepository
{
    private readonly IMongoCollection<InstitutionDocument> _collection;

    public MongoInstitutionRepository(IMongoDatabase database) =>
        _collection = database.GetCollection<InstitutionDocument>("instituciones");

    public async Task<Institution?> FindAsync(string codigoOficial, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(d => d.Id == codigoOficial).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : Institution.Create(document.Id, document.Nombre);
    }

    public async Task SaveAsync(Institution institution, CancellationToken cancellationToken = default)
    {
        var document = new InstitutionDocument { Id = institution.Id, Nombre = institution.Nombre };
        await _collection.ReplaceOneAsync(
            d => d.Id == document.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private sealed class InstitutionDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;
    }
}
