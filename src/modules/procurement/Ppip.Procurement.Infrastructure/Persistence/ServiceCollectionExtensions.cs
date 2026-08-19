using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Ppip.BuildingBlocks.Messaging;
using Ppip.Procurement.Domain.Ports;

namespace Ppip.Procurement.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra los adaptadores Mongo de todos los puertos de <c>Ppip.Procurement.Domain/Ports</c> más <see cref="IOutboxStore"/> (FASE 6).</summary>
    public static IHostApplicationBuilder AddProcurementPersistence(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<MongoOptions>()
            .Bind(builder.Configuration.GetSection(MongoOptions.SectionName));

        builder.Services.AddSingleton<ProcurementMongoDatabaseProvider>();

        builder.Services.AddSingleton<ICompraAgilRepository>(sp => new MongoCompraAgilRepository(Db(sp)));
        builder.Services.AddSingleton<IInstitutionRepository>(sp => new MongoInstitutionRepository(Db(sp)));
        builder.Services.AddSingleton<ISyncCheckpointRepository>(sp => new MongoSyncCheckpointRepository(Db(sp)));
        builder.Services.AddSingleton<ISyncExecutionRepository>(sp => new MongoSyncExecutionRepository(Db(sp)));
        builder.Services.AddSingleton<IRawPayloadRepository>(sp => new MongoRawPayloadRepository(Db(sp)));
        builder.Services.AddSingleton<IOutboxStore>(sp => new MongoOutboxStore(Db(sp)));

        return builder;
    }

    private static IMongoDatabase Db(IServiceProvider sp) => sp.GetRequiredService<ProcurementMongoDatabaseProvider>().Database;
}
