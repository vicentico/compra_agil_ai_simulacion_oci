using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ppip.BuildingBlocks.Health;
using Ppip.DocumentWorker;

// ============================================================================
// Ppip.DocumentWorker — esqueleto FASE 1. Pipeline real en FASE 7-9.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var mongoConnectionString = config["Ppip:Mongo:ConnectionString"] ?? string.Empty;
var rabbitHost = config["Ppip:RabbitMq:Host"] ?? string.Empty;
var rabbitUser = config["Ppip:RabbitMq:Username"] ?? string.Empty;
var rabbitPassword = config["Ppip:RabbitMq:Password"] ?? string.Empty;
var minioEndpoint = config["Ppip:MinIo:Endpoint"] ?? string.Empty;
var qdrantEndpoint = config["Ppip:Qdrant:Endpoint"] ?? string.Empty;

builder.Services.AddHttpClient();
builder.Services.AddHostedService<HeartbeatWorker>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck("mongodb", new MongoPingHealthCheck(mongoConnectionString), tags: ["ready"])
    .AddCheck("rabbitmq", new RabbitMqHealthCheck(rabbitHost, rabbitUser, rabbitPassword), tags: ["ready"])
    .AddCheck(
        "minio",
        sp => new HttpEndpointHealthCheck(
            sp.GetRequiredService<IHttpClientFactory>(), "MinIO", $"{minioEndpoint}/minio/health/live"),
        tags: ["ready"])
    .AddCheck(
        "qdrant",
        sp => new HttpEndpointHealthCheck(
            sp.GetRequiredService<IHttpClientFactory>(), "Qdrant", $"{qdrantEndpoint}/healthz"),
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
