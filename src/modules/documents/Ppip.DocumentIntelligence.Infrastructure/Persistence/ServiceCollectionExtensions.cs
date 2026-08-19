using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Ppip.BuildingBlocks.Messaging;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddDocumentPersistence(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<MongoOptions>()
            .Bind(builder.Configuration.GetSection(MongoOptions.SectionName));

        builder.Services.AddSingleton<DocumentMongoDatabaseProvider>();

        builder.Services.AddSingleton<IDocumentRepository>(sp => new MongoDocumentRepository(Db(sp)));
        builder.Services.AddSingleton<IDocumentChunkRepository>(sp => new MongoDocumentChunkRepository(Db(sp)));
        builder.Services.AddSingleton<IOutboxStore>(sp => new MongoOutboxStore(Db(sp)));

        return builder;
    }

    private static IMongoDatabase Db(IServiceProvider sp) => sp.GetRequiredService<DocumentMongoDatabaseProvider>().Database;
}
