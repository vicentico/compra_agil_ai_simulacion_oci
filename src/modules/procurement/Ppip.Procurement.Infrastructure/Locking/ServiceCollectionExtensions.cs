using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ppip.Procurement.Domain.Ports;
using StackExchange.Redis;

namespace Ppip.Procurement.Infrastructure.Locking;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddSyncLock(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<RedisOptions>()
            .Bind(builder.Configuration.GetSection(RedisOptions.SectionName));

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.ConnectionString);
        });

        builder.Services.AddSingleton<ISyncLock, RedisSyncLock>();

        return builder;
    }
}
