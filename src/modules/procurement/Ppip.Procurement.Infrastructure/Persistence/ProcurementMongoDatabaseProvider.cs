using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ppip.Procurement.Infrastructure.Persistence;

/// <summary>
/// Envoltorio deliberado en vez de registrar <see cref="IMongoDatabase"/>
/// directamente en el contenedor DI (como se hacía hasta FASE 8): FASE 9
/// combina por primera vez Procurement con DocumentIntelligence y Knowledge
/// en el mismo proceso (<c>Ppip.PlatformApi</c>, para UC-005), y cada módulo
/// apunta a una base lógica distinta ("procurement" vs "documents" vs
/// "knowledge") — un <c>AddSingleton&lt;IMongoDatabase&gt;</c> por módulo
/// colisionaría (la última resolución gana para CUALQUIER consumidor de
/// <see cref="IMongoDatabase"/>, apuntando silenciosamente a la base
/// equivocada). Este tipo hace único el registro; mismo patrón que
/// <c>Ppip.Knowledge.Infrastructure.Persistence.KnowledgeMongoDatabaseProvider</c>.
/// </summary>
public sealed class ProcurementMongoDatabaseProvider
{
    public IMongoDatabase Database { get; }

    public ProcurementMongoDatabaseProvider(IOptions<MongoOptions> options)
    {
        var opts = options.Value;
        Database = new MongoClient(opts.ConnectionString).GetDatabase(opts.DatabaseName);
    }
}
