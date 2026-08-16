using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ppip.AiWorker;
using Ppip.BuildingBlocks.Health;

// ============================================================================
// Ppip.AiWorker — esqueleto FASE 1. ILlmProvider/IEmbeddingProvider reales
// en FASE 9-10 (ADR-007).
// ============================================================================

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var mongoConnectionString = config["Ppip:Mongo:ConnectionString"] ?? string.Empty;
var rabbitHost = config["Ppip:RabbitMq:Host"] ?? string.Empty;
var rabbitUser = config["Ppip:RabbitMq:Username"] ?? string.Empty;
var rabbitPassword = config["Ppip:RabbitMq:Password"] ?? string.Empty;
var qdrantEndpoint = config["Ppip:Qdrant:Endpoint"] ?? string.Empty;
var ollamaEndpoint = config["Ppip:Ollama:Endpoint"] ?? string.Empty;

builder.Services.AddHttpClient();
builder.Services.AddHostedService<HeartbeatWorker>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck("mongodb", new MongoPingHealthCheck(mongoConnectionString), tags: ["ready"])
    .AddCheck("rabbitmq", new RabbitMqHealthCheck(rabbitHost, rabbitUser, rabbitPassword), tags: ["ready"])
    .AddCheck(
        "qdrant",
        sp => new HttpEndpointHealthCheck(
            sp.GetRequiredService<IHttpClientFactory>(), "Qdrant", $"{qdrantEndpoint}/healthz"),
        tags: ["ready"])
    .AddCheck(
        "ollama",
        sp => new HttpEndpointHealthCheck(
            sp.GetRequiredService<IHttpClientFactory>(), "Ollama", $"{ollamaEndpoint}/api/tags"),
        tags: ["ready"]);

var app = builder.Build();
var jsonWriter = new HealthCheckJsonWriter();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = jsonWriter.WriteAsync,
});
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = jsonWriter.WriteAsync,
});

app.Run();

internal sealed class HealthCheckJsonWriter
{
    public Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
            }),
        });
        return context.Response.WriteAsync(payload);
    }
}
