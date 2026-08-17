using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ppip.Procurement.Infrastructure.Messaging;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddOutboxDispatcher(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<RabbitMqOptions>()
            .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

        builder.Services.AddHostedService<OutboxDispatcher>();

        return builder;
    }
}
