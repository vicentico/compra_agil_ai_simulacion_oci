using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Minio;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Storage;

public static class ServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddDocumentStorage(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<MinioOptions>()
            .Bind(builder.Configuration.GetSection(MinioOptions.SectionName));

        builder.Services.AddSingleton<IMinioClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
            var uri = new Uri(options.Endpoint);
            return new MinioClient()
                .WithEndpoint(uri.Host, uri.Port)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(uri.Scheme == Uri.UriSchemeHttps)
                .Build();
        });

        builder.Services.AddSingleton<IObjectStorage, MinioObjectStorage>();

        return builder;
    }
}
