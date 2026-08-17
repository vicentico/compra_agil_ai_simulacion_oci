using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ppip.Procurement.Application.Mapping;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Domain.Ports;
using Ppip.Procurement.Infrastructure.ChileCompra;
using Ppip.Procurement.Infrastructure.ChileCompra.Dto;

namespace Ppip.Procurement.Application;

/// <summary>
/// Orquesta un ciclo completo de UC-001: lock distribuido (A5) → checkpoint →
/// paginado de <see cref="IChileCompraClient"/> → captura de raw → normaliza
/// → <see cref="SyncPolicy"/> decide → aplica al agregado → publica evento →
/// avanza checkpoint solo si el ciclo completo tuvo éxito. Componente
/// "SyncOrchestrator" de docs/04-architecture/03-component-diagram.md.
/// </summary>
public sealed class SyncOrchestrator(
    IChileCompraClient client,
    ICompraAgilRepository compras,
    IInstitutionRepository institutions,
    ISyncCheckpointRepository checkpoints,
    ISyncExecutionRepository executions,
    IRawPayloadRepository rawPayloads,
    ISyncLock syncLock,
    ProcurementEventPublisher publisher,
    IOptions<SyncOptions> options,
    ILogger<SyncOrchestrator> logger)
{
    public async Task<SyncExecution> RunAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        var execution = SyncExecution.Start(correlationId);

        await using var handle = await syncLock.TryAcquireAsync(opts.Source, opts.LockTtl, cancellationToken);
        if (handle is null)
        {
            // UC-001 A5: segundo ciclo concurrente termina de inmediato, sin tocar checkpoint ni datos.
            logger.LogInformation("Ciclo de sync {CorrelationId} omitido: {Source} ya tiene un ciclo en curso.", correlationId, opts.Source);
            execution.MarkSkipped();
            await executions.SaveAsync(execution, cancellationToken);
            return execution;
        }

        var checkpoint = await checkpoints.FindAsync(opts.Source, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var windowStart = checkpoint?.LastSuccessfulSync ?? now - opts.InitialWindowLookback;
        checkpoint ??= SyncCheckpoint.Initial(opts.Source, windowStart, now);

        try
        {
            var page = 1;
            int totalPages;
            do
            {
                var query = new CompraAgilListQuery
                {
                    CambioDesde = windowStart,
                    CambioHasta = now,
                    TamanoPagina = opts.PageSize,
                    NumeroPagina = page,
                    OrdenarPorCampo = OrdenarPor.FechaUltimaModificacion,
                };

                var listPayload = await client.ListarAsync(query, cancellationToken);
                totalPages = Math.Max(listPayload.Paginacion.TotalPaginas, 1);

                foreach (var item in listPayload.Items)
                {
                    await ProcessItemAsync(item, execution, correlationId, opts.Producer, cancellationToken);
                }

                page++;
            }
            while (page <= totalPages);

            // Checkpoint solo avanza tras un ciclo íntegro (UC-001 postcondición) — un ciclo abortado a mitad de camino no lo toca, el siguiente retoma la misma ventana.
            checkpoint.Advance(now, windowStart, now);
            await checkpoints.SaveAsync(checkpoint, cancellationToken);
            execution.Complete();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // F1/F2/F3: ChileCompra caído, 429 tras agotar reintentos, o error
            // inesperado — el ciclo actual se aborta con gracia (checkpoint
            // intacto); el worker sigue vivo para el próximo ciclo programado.
            logger.LogError(ex, "Ciclo de sync {CorrelationId} abortado.", correlationId);
            execution.Abort();
        }
        finally
        {
            await executions.SaveAsync(execution, cancellationToken);
        }

        return execution;
    }

    private async Task ProcessItemAsync(
        CompraAgilListItemDto item,
        SyncExecution execution,
        string correlationId,
        string producer,
        CancellationToken cancellationToken)
    {
        var rawPayloadId = Guid.CreateVersion7();
        var rawJson = JsonSerializer.Serialize(item);
        var raw = RawCompraAgilPayload.Capture(
            rawJson,
            sourceUrl: $"https://api2.mercadopublico.cl/v2/compra-agil/{item.Codigo}",
            DateTimeOffset.UtcNow,
            httpStatus: 200,
            apiVersion: "v2",
            correlationId);
        await rawPayloads.SaveAsync(rawPayloadId, item.Codigo, raw, cancellationToken);

        var normalized = CompraAgilNormalizer.Normalize(item);
        if (!normalized.Success)
        {
            // F4: raw ya quedó guardado (arriba); el registro normalizado no
            // se escribe — el resto de la página continúa.
            logger.LogWarning("Compra {Codigo} en cuarentena: {Errores}", item.Codigo, string.Join("; ", normalized.Errors));
            execution.RecordError();
            return;
        }

        await UpsertInstitutionAsync(normalized.Institution!, cancellationToken);

        var normalizedHash = NormalizedFieldsHasher.Compute(normalized.Titulo!, normalized.MontoEstimado!, normalized.Vigencia!, normalized.Estado!.Value);
        var existing = await compras.FindAsync(normalized.Id!, cancellationToken);
        var decision = SyncPolicy.Decide(existing, normalizedHash);

        switch (decision)
        {
            case SyncDecision.Create:
                var created = CompraAgil.Detect(
                    normalized.Id!,
                    normalized.Institution!,
                    normalized.Titulo!,
                    normalized.MontoEstimado!,
                    normalized.Vigencia!,
                    normalizedHash,
                    requirements: [],
                    correlationId);
                created.AlinearEstado(normalized.Estado!.Value);

                await compras.SaveAsync(created, cancellationToken);
                await publisher.PublishDetectedAsync(created, rawPayloadId, correlationId, producer, cancellationToken);
                execution.RecordCreated();
                break;

            case SyncDecision.Update:
                var before = (existing!.Titulo, existing.MontoEstimado, existing.Vigencia, existing.Estado);
                existing.ApplyUpdate(normalized.Titulo!, normalized.MontoEstimado!, normalized.Vigencia!, normalizedHash, existing.Requirements, correlationId);
                existing.AlinearEstado(normalized.Estado!.Value);

                // Se recalcula aquí (no se reutiliza el ChangedFields interno
                // de ApplyUpdate) porque ese solo compara título/monto/
                // vigencia — la alineación de Estado ocurre después y por
                // separado (AlinearEstado no levanta IDomainEvent). Ver
                // NormalizedFieldsHasher: por construcción, si decision==Update
                // al menos uno de estos 4 cambió, así que la lista nunca sale
                // vacía (CompraAgilUpdated.v1 exige changedFields no vacío).
                var changedFields = new List<string>();
                if (before.Titulo != existing.Titulo)
                {
                    changedFields.Add(nameof(CompraAgil.Titulo));
                }

                if (before.MontoEstimado != existing.MontoEstimado)
                {
                    changedFields.Add(nameof(CompraAgil.MontoEstimado));
                }

                if (before.Vigencia != existing.Vigencia)
                {
                    changedFields.Add(nameof(CompraAgil.Vigencia));
                }

                if (before.Estado != existing.Estado)
                {
                    changedFields.Add(nameof(CompraAgil.Estado));
                }

                await compras.SaveAsync(existing, cancellationToken);
                await publisher.PublishUpdatedAsync(existing, changedFields, rawPayloadId, correlationId, producer, cancellationToken);
                execution.RecordUpdated();
                break;

            case SyncDecision.NoOp:
                execution.RecordUnchanged();
                break;
        }
    }

    private async Task UpsertInstitutionAsync(InstitutionRef institutionRef, CancellationToken cancellationToken)
    {
        var existing = await institutions.FindAsync(institutionRef.Id, cancellationToken);
        if (existing is null)
        {
            await institutions.SaveAsync(Institution.Create(institutionRef.Id, institutionRef.Name), cancellationToken);
            return;
        }

        if (existing.Nombre != institutionRef.Name)
        {
            existing.Renombrar(institutionRef.Name);
            await institutions.SaveAsync(existing, cancellationToken);
        }
    }
}
