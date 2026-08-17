using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ppip.Procurement.Application.Tests.Fakes;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Infrastructure.ChileCompra.Dto;
using Xunit;

namespace Ppip.Procurement.Application.Tests;

/// <summary>
/// Prueba UC-001 completo con dobles en memoria (sin Docker) — en particular
/// NFR-001: "re-ejecutar un sync sobre los mismos datos no crea duplicados
/// ni versiones espurias".
/// </summary>
public class SyncOrchestratorTests
{
    private static CompraAgilListItemDto Item(
        string codigo = "418-1191-COT26",
        string nombre = "KCR-OLOPATADINA 0,2% COLIRIO",
        string estadoCodigo = "publicada",
        decimal monto = 2_000_000m) => new()
    {
        Codigo = codigo,
        Nombre = nombre,
        Estado = new EstadoDto { IdEstado = 2, Codigo = estadoCodigo, Glosa = estadoCodigo },
        Fechas = new FechasListDto { FechaPublicacion = "2026-08-16 23:02", FechaCierre = "2026-08-18 08:30" },
        Montos = new MontosDto { Moneda = "CLP", MontoDisponible = monto },
        Institucion = new InstitucionDto { OrganismoComprador = "HOSPITAL DE EJEMPLO", Rut = "61.602.279-2" },
    };

    private sealed class Harness
    {
        public FakeChileCompraClient Client { get; } = new();
        public InMemoryCompraAgilRepository Compras { get; } = new();
        public InMemoryInstitutionRepository Institutions { get; } = new();
        public InMemorySyncCheckpointRepository Checkpoints { get; } = new();
        public InMemorySyncExecutionRepository Executions { get; } = new();
        public InMemoryRawPayloadRepository RawPayloads { get; } = new();
        public InMemoryOutboxStore Outbox { get; } = new();
        public InMemorySyncLock Lock { get; set; } = new();

        public SyncOrchestrator Build()
        {
            var options = Options.Create(new SyncOptions { PageSize = 50 });
            var publisher = new ProcurementEventPublisher(Outbox);
            return new SyncOrchestrator(
                Client, Compras, Institutions, Checkpoints, Executions, RawPayloads, Lock, publisher, options,
                NullLogger<SyncOrchestrator>.Instance);
        }
    }

    [Fact]
    public async Task FirstRun_CreatesCompraAndPublishesDetected()
    {
        var harness = new Harness();
        harness.Client.Items.Add(Item());
        var orchestrator = harness.Build();

        var execution = await orchestrator.RunAsync("corr-1");

        Assert.Equal(SyncExecutionStatus.Completed, execution.Status);
        Assert.Equal(1, execution.Created);
        Assert.Equal(1, harness.Compras.SaveCount);
        var message = Assert.Single(harness.Outbox.Messages);
        Assert.Equal("CompraAgilDetected", message.EventType);
        Assert.Equal("procurement.compra-agil-detected.v1", message.RoutingKey);
    }

    [Fact]
    public async Task SecondRun_SameData_IsIdempotent()
    {
        var harness = new Harness();
        harness.Client.Items.Add(Item());
        var orchestrator = harness.Build();

        await orchestrator.RunAsync("corr-1");
        harness.Outbox.Messages.Clear();
        var second = await orchestrator.RunAsync("corr-2");

        Assert.Equal(SyncExecutionStatus.Completed, second.Status);
        Assert.Equal(0, second.Created);
        Assert.Equal(0, second.Updated);
        Assert.Equal(1, second.Unchanged);
        Assert.Equal(1, harness.Compras.SaveCount); // sin escritura nueva
        Assert.Empty(harness.Outbox.Messages); // sin evento nuevo
    }

    [Fact]
    public async Task RunWhenAlreadyLocked_MarksSkipped_TouchesNothing()
    {
        var harness = new Harness();
        harness.Client.Items.Add(Item());
        var options = Options.Create(new SyncOptions());
        var orchestrator = new SyncOrchestrator(
            harness.Client, harness.Compras, harness.Institutions, harness.Checkpoints, harness.Executions,
            harness.RawPayloads, new AlwaysLockedSyncLock(), new ProcurementEventPublisher(harness.Outbox), options,
            NullLogger<SyncOrchestrator>.Instance);

        var execution = await orchestrator.RunAsync("corr-1");

        Assert.Equal(SyncExecutionStatus.Skipped, execution.Status);
        Assert.Equal(0, harness.Compras.SaveCount);
        Assert.Empty(harness.Outbox.Messages);
        Assert.Equal(0, harness.Client.CallCount);
    }

    [Fact]
    public async Task EstadoChange_PublishesUpdatedWithEstadoInChangedFields()
    {
        var harness = new Harness();
        harness.Client.Items.Add(Item(estadoCodigo: "publicada"));
        var orchestrator = harness.Build();
        await orchestrator.RunAsync("corr-1");
        harness.Outbox.Messages.Clear();

        harness.Client.Items[0] = Item(estadoCodigo: "cerrada");
        var second = await orchestrator.RunAsync("corr-2");

        Assert.Equal(1, second.Updated);
        var message = Assert.Single(harness.Outbox.Messages);
        Assert.Equal("CompraAgilUpdated", message.EventType);
        Assert.Contains("Estado", message.PayloadJson);
        Assert.Contains("\"changedFields\":[\"Estado\"]", message.PayloadJson);
    }

    [Fact]
    public async Task MalformedItem_QuarantinesAndContinuesWithRest()
    {
        var harness = new Harness();
        harness.Client.Items.Add(Item(codigo: "malformed", estadoCodigo: "no-existe"));
        harness.Client.Items.Add(Item(codigo: "418-1191-COT26"));
        var orchestrator = harness.Build();

        var execution = await orchestrator.RunAsync("corr-1");

        Assert.Equal(1, execution.Errors);
        Assert.Equal(1, execution.Created);
        Assert.Equal(1, harness.Compras.SaveCount);
        Assert.Equal(2, harness.RawPayloads.SaveCount); // el crudo se guarda igual (F4), aunque se ponga en cuarentena
    }

    [Fact]
    public async Task ChileCompraFails_AbortsGracefully_ChecpointNotAdvanced()
    {
        var harness = new Harness();
        harness.Client.ThrowOnNextCall = new HttpRequestException("simulated 500 after retries exhausted");
        var orchestrator = harness.Build();

        var execution = await orchestrator.RunAsync("corr-1");

        Assert.Equal(SyncExecutionStatus.Aborted, execution.Status);
        Assert.Null(await harness.Checkpoints.FindAsync("chilecompra"));
    }
}
