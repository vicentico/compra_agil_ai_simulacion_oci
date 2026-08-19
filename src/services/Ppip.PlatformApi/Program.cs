using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Ppip.BuildingBlocks.Health;
using Ppip.BuildingBlocks.Observability;
using Ppip.BuildingBlocks.Security;
using Ppip.DocumentIntelligence.Infrastructure.Persistence;
using Ppip.Knowledge.Application;
using Ppip.Knowledge.Application.Exceptions;
using Ppip.Knowledge.Domain.Exceptions;
using Ppip.Knowledge.Infrastructure;
using Ppip.Procurement.Infrastructure.Persistence;

// ============================================================================
// Ppip.PlatformApi — esqueleto FASE 1 (Docker infrastructure) + observabilidad
// FASE 2 (OTel, correlationId) + identidad/seguridad FASE 3 (JWT+RBAC) +
// primer endpoint de negocio real, UC-005 Consultar RAG (FASE 9).
//
// El resto de módulos de dominio (Procurement/Document más allá de lo que
// UC-005 necesita, Proposal, Compliance, Audit) siguen sin exponerse acá —
// hasta FASE 8 este Program.cs era deliberadamente un esqueleto (topología +
// observabilidad + RBAC, ver notas de cierre de FASE 1-3); UC-005 es el
// primer caso de uso de lectura completo, y por eso el primero que amerita
// componer los módulos de dominio en este proceso.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);
builder.AddPpipObservability("ppip-platform-api");
builder.AddPpipKeycloakAuth();

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

builder.AddProcurementPersistence();
builder.AddDocumentPersistence();
builder.AddKnowledgeEmbeddings();
builder.AddKnowledgeLlm();
builder.AddKnowledgeVectorIndex();
builder.AddKnowledgePersistence();
builder.Services.AddOptions<RagQueryOptions>().Bind(config.GetSection(RagQueryOptions.SectionName));
builder.Services.AddSingleton<RagQueryOrchestrator>();

builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddHttpClient("downstream-services")
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

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
    .AddTypeActivatedCheck<HttpEndpointHealthCheck>(
        "minio", failureStatus: null, tags: ["ready"], args: ["MinIO", $"{minioEndpoint}/minio/health/live"])
    .AddTypeActivatedCheck<HttpEndpointHealthCheck>(
        "qdrant", failureStatus: null, tags: ["ready"], args: ["Qdrant", $"{qdrantEndpoint}/healthz"]);

var app = builder.Build();

app.UseCorrelationId();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

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
    phase = "FASE 3 — Identity & security",
    status = "skeleton",
    docs = "docs/04-architecture/00-architecture-overview.md",
}));

// Endpoint de diagnóstico de FASE 3 (no de negocio): expone las claims/roles
// del token validado, para verificar manualmente el RBAC (criterio "viewer"
// — el rol mínimo, cualquier usuario autenticado lo satisface).
app.MapGet("/api/diagnostics/whoami", (HttpContext context) => Results.Ok(new
{
    subject = context.User.FindFirst("sub")?.Value,
    preferredUsername = context.User.FindFirst("preferred_username")?.Value,
    roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value),
})).RequireAuthorization(PpipRoles.Viewer);

// Endpoint de diagnóstico de FASE 2 (no de negocio): fuerza una llamada HTTP
// real hacia los 3 workers para demostrar el trace end-to-end + propagación
// de correlationId exigidos por el criterio de éxito de la fase
// (docs/ROADMAP.md). Las URLs quedan fijas a los nombres de servicio del
// docker-compose porque este endpoint es temporal — se reemplaza por el
// flujo real orientado a eventos a partir de FASE 4 (ver docs/03-domain/).
// Protegido con rol "analyst" (FASE 3): equivalente a los endpoints de
// estado/diagnóstico reales del catálogo (p.ej. GET /api/sync/status).
app.MapGet("/api/diagnostics/trace-check", async (
    IHttpClientFactory httpClientFactory,
    HttpContext context,
    CancellationToken ct) =>
{
    var client = httpClientFactory.CreateClient("downstream-services");
    (string Name, string Url)[] targets =
    [
        ("sync-worker", "http://sync-worker:8080/health"),
        ("document-worker", "http://document-worker:8080/health"),
        ("ai-worker", "http://ai-worker:8080/health"),
    ];

    var downstream = new List<object>();
    foreach (var (name, url) in targets)
    {
        try
        {
            var response = await client.GetAsync(url, ct);
            downstream.Add(new
            {
                service = name,
                statusCode = (int)response.StatusCode,
                healthy = response.IsSuccessStatusCode,
            });
        }
        catch (Exception ex)
        {
            downstream.Add(new { service = name, statusCode = 0, healthy = false, error = ex.Message });
        }
    }

    return Results.Ok(new
    {
        correlationId = context.Items[CorrelationIdMiddleware.HeaderName],
        traceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
        downstream,
        purpose = "FASE 2 — verifica trace+correlationId end-to-end (docs/13-observability/01-observability-spec.md)",
    });
}).RequireAuthorization(PpipRoles.Analyst);

// UC-005 (Consultar RAG, FASE 9) — primer endpoint de negocio de PlatformApi.
// compraId lo inyecta el servidor desde la ruta: NUNCA se toma del body
// (ADR-008, docs/06-api/01-example-rag-query.md). Rol mínimo "viewer" —
// cualquier usuario autenticado puede consultar.
app.MapPost("/api/rag/{compraId}/query", async (
    string compraId,
    RagQueryApiRequest request,
    RagQueryOrchestrator orchestrator,
    IOptions<RagQueryOptions> options,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    var correlationId = context.Items[CorrelationIdMiddleware.HeaderName] as string ?? Guid.CreateVersion7().ToString();
    var topK = request.TopK ?? options.Value.DefaultTopK;

    try
    {
        var query = new RagQueryRequest(compraId, request.Question ?? string.Empty, topK);
        var result = await orchestrator.QueryAsync(query, correlationId, cancellationToken);

        return Results.Ok(new
        {
            answer = result.Answer,
            answerType = result.AnswerType.ToString().ToUpperInvariant(),
            evidence = result.Evidence.Select(e => new
            {
                documentId = e.DocumentId,
                documentVersion = e.DocumentVersion,
                documentName = e.DocumentName,
                page = e.Page,
                chunkId = e.ChunkId,
                sourceText = e.SourceText,
                score = e.Score,
                confidence = e.Confidence,
            }),
            unanswered = result.Unanswered,
            execution = result.Execution is null ? null : new
            {
                model = result.Execution.Model,
                promptVersion = result.Execution.PromptVersion,
                tokensIn = result.Execution.TokensIn,
                tokensOut = result.Execution.TokensOut,
                latencyMs = result.Execution.LatencyMs,
            },
            correlationId = result.CorrelationId,
        });
    }
    catch (ArgumentException ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Solicitud inválida");
    }
    catch (CompraNotFoundException ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound, title: "Compra Ágil no encontrada");
    }
    catch (RetrievalUnavailableException ex)
    {
        // UC-005 A2: error explícito, detail identifica la dependencia caída (docs/06-api/01).
        return Results.Problem(detail: $"qdrant: {ex.Message}", statusCode: StatusCodes.Status503ServiceUnavailable, title: "Búsqueda vectorial no disponible");
    }
}).RequireAuthorization(PpipRoles.Viewer);

app.Run();

internal sealed record RagQueryApiRequest(string? Question, int? TopK);

// WebApplicationFactory<Program> (tests/Ppip.PlatformApi.Tests) necesita que
// la clase Program generada implícitamente por top-level statements sea
// pública.
public partial class Program;

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
