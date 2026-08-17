using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ppip.BuildingBlocks.Health;
using Ppip.BuildingBlocks.Observability;
using Ppip.Procurement.Application;
using Ppip.Procurement.Infrastructure.ChileCompra;
using Ppip.Procurement.Infrastructure.Locking;
using Ppip.Procurement.Infrastructure.Messaging;
using Ppip.Procurement.Infrastructure.Persistence;
using Ppip.SyncWorker;

// ============================================================================
// Ppip.SyncWorker — UC-001 completo (FASE 6): SyncOrchestrator + checkpoint +
// eventos. Cliente ChileCompra resiliente y observabilidad vienen de FASE 2/5.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);
builder.AddPpipObservability("ppip-sync-worker");
var config = builder.Configuration;
var mongoConnectionString = config["Ppip:Mongo:ConnectionString"] ?? string.Empty;
var rabbitHost = config["Ppip:RabbitMq:Host"] ?? string.Empty;
var rabbitUser = config["Ppip:RabbitMq:Username"] ?? string.Empty;
var rabbitPassword = config["Ppip:RabbitMq:Password"] ?? string.Empty;
var redisConnectionString = config["Ppip:Redis:ConnectionString"] ?? string.Empty;

builder.AddChileCompraClient();
builder.AddProcurementPersistence();
builder.AddSyncLock();
builder.AddOutboxDispatcher();

builder.Services.AddOptions<SyncOptions>()
    .Bind(config.GetSection(SyncOptions.SectionName));
builder.Services.AddSingleton<ProcurementEventPublisher>();
builder.Services.AddSingleton<SyncOrchestrator>();
builder.Services.AddHostedService<SyncSchedulerWorker>();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck("mongodb", new MongoPingHealthCheck(mongoConnectionString), tags: ["ready"])
    .AddCheck("rabbitmq", new RabbitMqHealthCheck(rabbitHost, rabbitUser, rabbitPassword), tags: ["ready"])
    .AddCheck("redis", new RedisHealthCheck(redisConnectionString), tags: ["ready"]);

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

// Disparo manual del ciclo de sync (UC-001 flujo principal, paso 1 — "o el
// administrador"). Sin auth propia: el endpoint autenticado (rol admin) que
// documenta docs/06-api/00 (`POST /api/sync/compra-agil`) vive en Platform
// API, que todavía no tiene wiring a este módulo — deliberadamente fuera de
// FASE 6 (ver docs/ROADMAP.md nota de cierre). Este es un endpoint interno
// del propio worker (no ruteado por Traefik) solo para disparo manual/demo.
app.MapPost("/internal/sync/trigger", async (SyncOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    var correlationId = $"sync-manual-{Guid.CreateVersion7()}";
    var execution = await orchestrator.RunAsync(correlationId, cancellationToken);
    return Results.Accepted(value: new
    {
        correlationId,
        execution.Status,
        execution.Created,
        execution.Updated,
        execution.Unchanged,
        execution.Errors,
    });
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
