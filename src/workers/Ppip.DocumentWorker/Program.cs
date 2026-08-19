using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using Ppip.Knowledge.Application;
using Ppip.Knowledge.Infrastructure;
using Ppip.Knowledge.Infrastructure.Embeddings;
using Ppip.Knowledge.Infrastructure.VectorIndex;

// ============================================================================
// Ppip.DocumentWorker — UC-003 pasos 1-3 (FASE 7: descarga validada SSRF,
// MinIO, hash) + pasos 4-9 (FASE 8: clasificación, extracción, OCR, chunking)
// + pasos 10-11 (FASE 9: embedding + indexing en Qdrant).
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
builder.AddKnowledgeEmbeddings();
builder.AddKnowledgeVectorIndex();
builder.AddKnowledgePersistence();

builder.Services.AddOptions<DocumentDownloadOptions>()
    .Bind(config.GetSection(DocumentDownloadOptions.SectionName));
builder.Services.AddOptions<DocumentProcessingOptions>()
    .Bind(config.GetSection(DocumentProcessingOptions.SectionName));
builder.Services.AddOptions<EmbeddingIndexingOptions>()
    .Bind(config.GetSection(EmbeddingIndexingOptions.SectionName));
builder.Services.AddSingleton<IMalwareScanner, NoOpMalwareScanner>();
builder.Services.AddSingleton<DocumentEventPublisher>();
builder.Services.AddSingleton<DocumentDownloadOrchestrator>();
builder.Services.AddSingleton<DocumentProcessingOrchestrator>();
// KnowledgeEventPublisher reusa el mismo IOutboxStore que DocumentEventPublisher
// (registrado por AddDocumentPersistence): IOutboxStore es un puerto genérico
// de Ppip.BuildingBlocks.Messaging, no específico de un módulo.
builder.Services.AddSingleton<KnowledgeEventPublisher>();
builder.Services.AddSingleton<EmbeddingIndexer>();

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

// A diferencia de Mongo (crea colecciones/índices implícitamente al primer
// write), Qdrant rechaza un upsert contra una colección inexistente — a
// diferencia del resto de los EnsureIndexesAsync del proyecto (nunca
// llamados en startup, solo ejercidos por tests), este SÍ se invoca acá
// porque sin él el pipeline de indexing fallaría siempre en un stack recién
// levantado. Un Qdrant caído en este momento no debe impedir que el worker
// levante (UC-005 A2 ya maneja Qdrant caído en tiempo de consulta/indexado).
try
{
    var qdrantOptions = app.Services.GetRequiredService<IOptions<QdrantOptions>>().Value;
    var embeddingOptions = app.Services.GetRequiredService<IOptions<EmbeddingProviderOptions>>().Value;
    using var provisioningClient = new HttpClient { BaseAddress = new Uri(qdrantOptions.Endpoint) };
    if (!string.IsNullOrWhiteSpace(qdrantOptions.ApiKey))
    {
        provisioningClient.DefaultRequestHeaders.Add("api-key", qdrantOptions.ApiKey);
    }

    await QdrantVectorIndex.EnsureCollectionAsync(provisioningClient, qdrantOptions, embeddingOptions.Dimension);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "No fue posible asegurar la colección Qdrant al arrancar (se reintentará en el primer indexing).");
}

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

// Disparo manual/demo de docs/09 etapas 10-11 (FASE 9) — mismo criterio que
// los dos endpoints anteriores: sin consumidor RabbitMQ real todavía, embebe
// e indexa los chunks pendientes de la versión actual de un documento ya
// chunkeado (UC-003 paso 9 completo).
app.MapPost("/internal/documents/{documentId}/index", async (string documentId, EmbeddingIndexer indexer, CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(documentId, out var parsedId))
    {
        return Results.BadRequest(new { error = "documentId debe ser un GUID válido." });
    }

    var correlationId = $"doc-index-manual-{Guid.CreateVersion7()}";
    var indexedCount = await indexer.IndexAsync(DocumentId.From(parsedId), correlationId, cancellationToken);
    return Results.Accepted(value: new
    {
        correlationId,
        documentId,
        indexedCount,
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
