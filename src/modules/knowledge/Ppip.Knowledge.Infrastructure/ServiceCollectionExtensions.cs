using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ppip.Knowledge.Domain.Ports;
using Ppip.Knowledge.Infrastructure.Embeddings;
using Ppip.Knowledge.Infrastructure.Llm;
using Ppip.Knowledge.Infrastructure.Persistence;
using Ppip.Knowledge.Infrastructure.VectorIndex;

namespace Ppip.Knowledge.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary><c>Ppip:Knowledge:Embeddings:Provider</c>: <c>Mock</c> (por defecto) o <c>Ollama</c> (real, <c>Ppip:Ollama:Endpoint</c>).</summary>
    public static IHostApplicationBuilder AddKnowledgeEmbeddings(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<EmbeddingProviderOptions>().Bind(builder.Configuration.GetSection(EmbeddingProviderOptions.SectionName));

        if (string.Equals(builder.Configuration[$"{EmbeddingProviderOptions.SectionName}:Provider"], "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddHttpClient<IEmbeddingProvider, OllamaEmbeddingProvider>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Ppip:Ollama:Endpoint"] ?? "http://localhost:11434");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
        }
        else
        {
            builder.Services.AddSingleton<IEmbeddingProvider, MockEmbeddingProvider>();
        }

        return builder;
    }

    /// <summary><c>Ppip:Knowledge:Llm:Provider</c>: <c>Mock</c> (por defecto) o <c>Ollama</c> (real, <c>Ppip:Ollama:Endpoint</c>).</summary>
    public static IHostApplicationBuilder AddKnowledgeLlm(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<LlmProviderOptions>().Bind(builder.Configuration.GetSection(LlmProviderOptions.SectionName));

        if (string.Equals(builder.Configuration[$"{LlmProviderOptions.SectionName}:Provider"], "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddHttpClient<ILlmProvider, OllamaLlmProvider>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Ppip:Ollama:Endpoint"] ?? "http://localhost:11434");
                // La síntesis puede tardar bastante más que un embedding — sin
                // streaming, el timeout debe cubrir la generación completa.
                client.Timeout = TimeSpan.FromSeconds(120);
            });
        }
        else
        {
            builder.Services.AddSingleton<ILlmProvider, MockLlmProvider>();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddKnowledgeVectorIndex(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<QdrantOptions>().Bind(builder.Configuration.GetSection(QdrantOptions.SectionName));

        builder.Services.AddHttpClient<IVectorIndex, QdrantVectorIndex>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint);
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("api-key", options.ApiKey);
            }
        });

        return builder;
    }

    public static IHostApplicationBuilder AddKnowledgePersistence(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<MongoOptions>().Bind(builder.Configuration.GetSection(MongoOptions.SectionName));
        builder.Services.AddSingleton<KnowledgeMongoDatabaseProvider>();
        builder.Services.AddSingleton<IEmbeddingRepository, MongoEmbeddingRepository>();
        builder.Services.AddSingleton<IAIExecutionRepository, MongoAIExecutionRepository>();

        return builder;
    }
}
