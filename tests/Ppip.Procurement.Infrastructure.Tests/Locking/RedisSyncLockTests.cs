using Ppip.Procurement.Infrastructure.Locking;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Ppip.Procurement.Infrastructure.Tests.Locking;

/// <summary>Contra un Redis real — valida la exclusión mutua de UC-001 A5 y que el release no borra un lock ajeno.</summary>
public sealed class RedisSyncLockTests : IAsyncLifetime
{
    // Misma imagen que infrastructure/docker/docker-compose.yml (servicio redis) — consistencia con el entorno real.
    private readonly RedisContainer _container = new RedisBuilder("redis:7.4-alpine").Build();
    private ConnectionMultiplexer _connection = null!;
    private RedisSyncLock _lock = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
        _lock = new RedisSyncLock(_connection);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquire_WhenFree_Succeeds()
    {
        await using var handle = await _lock.TryAcquireAsync("chilecompra", TimeSpan.FromMinutes(10));

        Assert.NotNull(handle);
    }

    [Fact]
    public async Task TryAcquire_WhenAlreadyHeld_ReturnsNull()
    {
        await using var first = await _lock.TryAcquireAsync("chilecompra", TimeSpan.FromMinutes(10));

        var second = await _lock.TryAcquireAsync("chilecompra", TimeSpan.FromMinutes(10));

        Assert.Null(second);
    }

    [Fact]
    public async Task AfterRelease_CanAcquireAgain()
    {
        var first = await _lock.TryAcquireAsync("chilecompra", TimeSpan.FromMinutes(10));
        await first!.DisposeAsync();

        await using var second = await _lock.TryAcquireAsync("chilecompra", TimeSpan.FromMinutes(10));

        Assert.NotNull(second);
    }

    [Fact]
    public async Task Release_DoesNotDeleteAnotherHolderKey()
    {
        // Simula: el TTL del lock original expiró y otro proceso ya lo tomó
        // (mismo key, token distinto) antes de que el handle original se
        // liberara — el release debe fallar el compare-and-delete y no
        // borrar el lock del segundo proceso.
        var handle = await _lock.TryAcquireAsync("chilecompra", TimeSpan.FromMinutes(10));
        Assert.NotNull(handle);

        var db = _connection.GetDatabase();
        await db.StringSetAsync("lock:sync:chilecompra", "otro-proceso", TimeSpan.FromMinutes(10));

        await handle!.DisposeAsync();

        var stillThere = await db.StringGetAsync("lock:sync:chilecompra");
        Assert.Equal("otro-proceso", stillThere.ToString());
    }
}
