using Ppip.BuildingBlocks.Messaging;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Domain.Ports;
using Ppip.Procurement.Infrastructure.ChileCompra;
using Ppip.Procurement.Infrastructure.ChileCompra.Dto;

namespace Ppip.Procurement.Application.Tests.Fakes;

/// <summary>Sirve páginas reales (con paginación) desde una lista en memoria — no hace HTTP ni consume cuota.</summary>
public sealed class FakeChileCompraClient : IChileCompraClient
{
    public List<CompraAgilListItemDto> Items { get; set; } = [];

    public int CallCount { get; private set; }

    public Exception? ThrowOnNextCall { get; set; }

    public Task<CompraAgilListPayloadDto> ListarAsync(CompraAgilListQuery query, CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (ThrowOnNextCall is { } exception)
        {
            ThrowOnNextCall = null;
            throw exception;
        }

        var pageSize = query.TamanoPagina;
        var totalPages = Math.Max((int)Math.Ceiling(Items.Count / (double)pageSize), 1);
        var pageItems = Items.Skip((query.NumeroPagina - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new CompraAgilListPayloadDto
        {
            Items = pageItems,
            Paginacion = new PaginacionDto
            {
                TotalPaginas = totalPages,
                NumeroPagina = query.NumeroPagina,
                TamanoPagina = pageSize,
                TotalResultados = Items.Count,
            },
        });
    }

    public Task<CompraAgilDetailDto> ObtenerDetalleAsync(string codigo, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("El orquestador de FASE 6 normaliza desde el listado, no llama al detalle.");
}

public sealed class InMemoryCompraAgilRepository : ICompraAgilRepository
{
    private readonly Dictionary<string, CompraAgil> _store = [];

    public int SaveCount { get; private set; }

    public Task<CompraAgil?> FindAsync(CompraAgilId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(id.Value));

    public Task SaveAsync(CompraAgil compra, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        _store[compra.Id.Value] = compra;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryInstitutionRepository : IInstitutionRepository
{
    private readonly Dictionary<string, Institution> _store = [];

    public Task<Institution?> FindAsync(string codigoOficial, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(codigoOficial));

    public Task SaveAsync(Institution institution, CancellationToken cancellationToken = default)
    {
        _store[institution.Id] = institution;
        return Task.CompletedTask;
    }
}

public sealed class InMemorySyncCheckpointRepository : ISyncCheckpointRepository
{
    private readonly Dictionary<string, SyncCheckpoint> _store = [];

    public Task<SyncCheckpoint?> FindAsync(string source, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(source));

    public Task SaveAsync(SyncCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        _store[checkpoint.Id] = checkpoint;
        return Task.CompletedTask;
    }
}

public sealed class InMemorySyncExecutionRepository : ISyncExecutionRepository
{
    public List<SyncExecution> Saved { get; } = [];

    public Task SaveAsync(SyncExecution execution, CancellationToken cancellationToken = default)
    {
        Saved.Add(execution);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryRawPayloadRepository : IRawPayloadRepository
{
    public int SaveCount { get; private set; }

    public Task SaveAsync(Guid rawPayloadId, string codigo, RawCompraAgilPayload payload, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryOutboxStore : IOutboxStore
{
    public List<OutboxMessage> Messages { get; } = [];

    public Task AppendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxMessage>>([.. Messages.Where(m => !m.IsPublished).Take(maxCount)]);

    public Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        Messages.Single(m => m.Id == messageId).MarkPublished(publishedAt);
        return Task.CompletedTask;
    }
}

/// <summary>Adquiere/libera de verdad (para el caso feliz) — simula la exclusión mutua real sin Redis.</summary>
public sealed class InMemorySyncLock : ISyncLock
{
    private readonly HashSet<string> _held = [];

    public Task<IAsyncDisposable?> TryAcquireAsync(string source, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (!_held.Add(source))
        {
            return Task.FromResult<IAsyncDisposable?>(null);
        }

        return Task.FromResult<IAsyncDisposable?>(new Handle(this, source));
    }

    private sealed class Handle(InMemorySyncLock owner, string source) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            owner._held.Remove(source);
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>Simula que otro ciclo ya está corriendo (UC-001 A5) — nunca entrega el lock.</summary>
public sealed class AlwaysLockedSyncLock : ISyncLock
{
    public Task<IAsyncDisposable?> TryAcquireAsync(string source, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        Task.FromResult<IAsyncDisposable?>(null);
}
