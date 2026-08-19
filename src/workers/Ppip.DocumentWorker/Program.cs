using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ppip.BuildingBlocks.Health;
using Ppip.BuildingBlocks.Observability;
using Ppip.DocumentIntelligence.Application;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;
using Ppip.DocumentIntelligence.Infrastructure;
using Ppip.DocumentIntelligence.Infrastructure.Http;
using Ppip.DocumentIntelligence.Infrastructure.Ocr;
using Ppip.DocumentIntelligence.Infrastructure.Persistence;
using Ppip.DocumentIntelligence.Infrastructure.Storage;
using Ppip.DocumentWorker;

// ============================================================================
// Ppip.DocumentWorker — UC-003 pasos 1-3 (FASE 7: descarga validada SSRF,
// MinIO, hash) + pasos 4-9 (FASE 8: clasificación, extracción, OCR, chunking).
// Embedding/Indexing (pasos 10-11): FASE 9.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);
builder.AddPpipObservability("ppip-document-worker");
var config = builder.Configuration;
var mongoConnectionString = config["Ppip:Mongo:ConnectionString"] ?? string.Empty;
var rabbitHost = config["Ppip:RabbitMq:Host"] ?? string.Empty;
var rabbitUser = config["Ppip:RabbitMq:Username"] ?? string.Empty;
var rabbitPassword = config["Ppip:RabbitMq:Password"] ?? string.Empty;
var minioEndpoint = config["Ppip:MinIo:Endpoint"] ?? string.Empty;
var qdrantEndpoint = config["Ppip:Qdrant:Endpoint"] ?? string.Empty;

builder.AddDocumentDownloader();
builder.AddDocumentStorage();
builder.AddDocumentPersistence();
builder.AddDocumentIntelligenceProcessing();

builder.Services.AddOptions<DocumentDownloadOptions>()
    .Bind(config.GetSection(DocumentDownloadOptions.SectionName));
builder.Services.AddOptions<DocumentProcessingOptions>()
    .Bind(config.GetSection(DocumentProcessingOptions.SectionName));
builder.Services.AddSingleton<IMalwareScanner, NoOpMalwareScanner>();
builder.Services.AddSingleton<DocumentEventPublisher>();
builder.Services.AddSingleton<DocumentDownloadOrchestrator>();
builder.Services.AddSingleton<DocumentProcessingOrchestrator>();

builder.Services.AddHostedService<HeartbeatWorker>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck("mongodb", new MongoPingHealthCheck(mongoConnectionString), tags: ["ready"])
    .AddCheck("rabbitmq", new RabbitMqHealthCheck(rabbitHost, rabbitUser, rabbitPassword), tags: ["ready"])
    .AddTypeActivatedCheck<HttpEndpointHealthCheck>(
        "minio", failureStatus: null, tags: ["ready"], args: ["MinIO", $"{minioEndpoint}/minio/health/live"])
    .AddTypeActivatedCheck<HttpEndpointHealthCheck>(
        "qdrant", failureStatus: null, tags: ["ready"], args: ["Qdrant", $"{qdrantEndpoint}/healthz"]);

var app = builder.Build();
app.UseCorrelationId();
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

// Disparo manual/demo (sin trigger automático real todavía: OQ-02 sigue
// abierta — no hay una fuente confirmada de sourceUrl reales de ChileCompra,
// ver docs/ROADMAP.md nota de cierre de FASE 7). Mismo criterio que
// Ppip.SyncWorker: endpoint interno, sin auth, no ruteado por Traefik.
app.MapPost("/internal/documents/download", async (DownloadRequest request, DocumentDownloadOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    var correlationId = $"doc-manual-{Guid.CreateVersion7()}";
    var document = await orchestrator.ProcessAsync(request.CompraAgilId, request.SourceUrl, request.DeclaredName, correlationId, cancellationToken);
    return Results.Accepted(value: new
    {
        correlationId,
        documentId = document.Id.ToString(),
        document.Stage,
        document.FailureReason,
    });
});

// Disparo manual/demo de UC-003 pasos 4-9 (FASE 8) — mismo criterio que el
// endpoint de descarga: sin consumidor RabbitMQ real todavía (docs/ROADMAP.md
// nota de cierre de FASE 8), procesa la versión actual ya descargada de un
// documento existente.
app.MapPost("/internal/documents/{documentId}/process", async (string documentId, DocumentProcessingOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(documentId, out var parsedId))
    {
        return Results.BadRequest(new { error = "documentId debe ser un GUID válido." });
    }

    var correlationId = $"doc-process-manual-{Guid.CreateVersion7()}";
    var document = await orchestrator.ProcessAsync(DocumentId.From(parsedId), correlationId, cancellationToken);
    var version = document.CurrentVersion;
    return Results.Accepted(value: new
    {
        correlationId,
        documentId = document.Id.ToString(),
        processingStage = version?.ProcessingStage,
        classification = version?.Classification,
        pageCount = version?.Pages.Count,
        failureReason = version?.ProcessingFailureReason,
    });
});

app.Run();

internal sealed record DownloadRequest(string CompraAgilId, string SourceUrl, string DeclaredName);

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
