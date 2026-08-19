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

        builder.Services.AddSingleton<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
            var client = new MongoClient(options.ConnectionString);
            return client.GetDatabase(options.DocumentsDatabaseName);
        });

        builder.Services.AddSingleton<IDocumentRepository, MongoDocumentRepository>();
        builder.Services.AddSingleton<IDocumentChunkRepository, MongoDocumentChunkRepository>();
        builder.Services.AddSingleton<IOutboxStore, MongoOutboxStore>();

        return builder;
    }
}
