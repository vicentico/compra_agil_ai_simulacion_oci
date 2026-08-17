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

        builder.Services.AddSingleton<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            var client = new MongoClient(options.ConnectionString);
            return client.GetDatabase(options.DatabaseName);
        });

        builder.Services.AddSingleton<ICompraAgilRepository, MongoCompraAgilRepository>();
        builder.Services.AddSingleton<IInstitutionRepository, MongoInstitutionRepository>();
        builder.Services.AddSingleton<ISyncCheckpointRepository, MongoSyncCheckpointRepository>();
        builder.Services.AddSingleton<ISyncExecutionRepository, MongoSyncExecutionRepository>();
        builder.Services.AddSingleton<IRawPayloadRepository, MongoRawPayloadRepository>();
        builder.Services.AddSingleton<IOutboxStore, MongoOutboxStore>();

        return builder;
    }
}
