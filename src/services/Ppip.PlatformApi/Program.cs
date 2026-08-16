using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ppip.BuildingBlocks.Health;

// ============================================================================
// Ppip.PlatformApi — esqueleto FASE 1 (Docker infrastructure).
//
// Módulos de dominio (Procurement, Document, Knowledge/RAG, Proposal,
// Compliance, Audit) se incorporan a partir de FASE 4 según docs/ROADMAP.md
// y docs/03-domain/. Este Program.cs solo demuestra que la topología de red
// y las credenciales de infraestructura están correctamente cableadas
// (criterio de éxito de FASE 1).
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
var mongoConnectionString = config["Ppip:Mongo:ConnectionString"] ?? string.Empty;
var redisConnectionString = config["Ppip:Redis:ConnectionString"] ?? string.Empty;
var rabbitHost = config["Ppip:RabbitMq:Host"] ?? string.Empty;
var rabbitUser = config["Ppip:RabbitMq:Username"] ?? string.Empty;
var rabbitPassword = config["Ppip:RabbitMq:Password"] ?? string.Empty;
var minioEndpoint = config["Ppip:MinIo:Endpoint"] ?? string.Empty;
var qdrantEndpoint = config["Ppip:Qdrant:Endpoint"] ?? string.Empty;
var allowedOrigins = (config["Ppip:Cors:AllowedOrigins"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// "live" = liveness pura (el proceso está vivo). "ready" (sin tag = todos los
// checks) = valida dependencias reales, per docs/13-observability/01.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck("mongodb", new MongoPingHealthCheck(mongoConnectionString), tags: ["ready"])
    .AddCheck("redis", new RedisHealthCheck(redisConnectionString), tags: ["ready"])
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

app.UseCors("Default");

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

app.MapGet("/", () => Results.Ok(new
{
    service = "ppip-platform-api",
    phase = "FASE 1 — Docker infrastructure",
    status = "skeleton",
    docs = "docs/04-architecture/00-architecture-overview.md",
}));

app.Run();

/// <summary>Formatea el resultado de health checks como JSON estructurado (NFR-003).</summary>
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
