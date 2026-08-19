using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ppip.Knowledge.Infrastructure.Persistence;

/// <summary>
/// Envoltorio deliberado en vez de registrar <see cref="IMongoDatabase"/>
/// directamente en el contenedor DI: <c>Ppip.DocumentWorker</c> (FASE 9)
/// combina por primera vez Knowledge con DocumentIntelligence en el mismo
/// proceso, y ambos módulos apuntan a bases lógicas distintas
/// ("knowledge" vs "documents") — un <c>AddSingleton&lt;IMongoDatabase&gt;</c>
/// de cada módulo colisionaría (la última resolución gana, apuntando
/// silenciosamente a la base equivocada). Este tipo hace único el registro.
/// </summary>
public sealed class KnowledgeMongoDatabaseProvider
{
    public IMongoDatabase Database { get; }

    public KnowledgeMongoDatabaseProvider(IOptions<MongoOptions> options)
    {
        var opts = options.Value;
        Database = new MongoClient(opts.ConnectionString).GetDatabase(opts.KnowledgeDatabaseName);
    }

    /// <summary>Usado por tests contra un <see cref="IMongoDatabase"/> ya resuelto (p.ej. Testcontainers).</summary>
    public KnowledgeMongoDatabaseProvider(IMongoDatabase database) => Database = database;
}
