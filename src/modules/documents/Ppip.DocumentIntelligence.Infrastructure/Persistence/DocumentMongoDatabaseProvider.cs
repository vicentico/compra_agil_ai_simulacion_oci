using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ppip.DocumentIntelligence.Infrastructure.Persistence;

/// <summary>
/// Envoltorio deliberado en vez de registrar <see cref="IMongoDatabase"/>
/// directamente en el contenedor DI: mismo motivo y mismo patrón que
/// <c>Ppip.Procurement.Infrastructure.Persistence.ProcurementMongoDatabaseProvider</c>
/// y <c>Ppip.Knowledge.Infrastructure.Persistence.KnowledgeMongoDatabaseProvider</c>
/// (FASE 9: <c>Ppip.PlatformApi</c> combina los tres módulos en un solo proceso).
/// </summary>
public sealed class DocumentMongoDatabaseProvider
{
    public IMongoDatabase Database { get; }

    public DocumentMongoDatabaseProvider(IOptions<MongoOptions> options)
    {
        var opts = options.Value;
        Database = new MongoClient(opts.ConnectionString).GetDatabase(opts.DocumentsDatabaseName);
    }
}
