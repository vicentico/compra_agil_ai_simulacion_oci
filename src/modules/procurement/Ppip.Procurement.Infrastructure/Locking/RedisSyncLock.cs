using Ppip.Procurement.Domain.Ports;
using StackExchange.Redis;

namespace Ppip.Procurement.Infrastructure.Locking;

/// <summary>
/// Lock distribuido vía Redis SETNX (docs/08-data: <c>lock:sync:{source}</c>)
/// — implementa <see cref="ISyncLock"/> (UC-001 A5). La liberación compara el
/// token propio antes de borrar (script Lua atómico) para no liberar un lock
/// ajeno si el TTL ya expiró y otro proceso lo tomó mientras tanto.
/// </summary>
public sealed class RedisSyncLock(IConnectionMultiplexer connection) : ISyncLock
{
    private static readonly LuaScript ReleaseScript = LuaScript.Prepare(
        "if redis.call('get', @key) == @token then return redis.call('del', @key) else return 0 end");

    public async Task<IAsyncDisposable?> TryAcquireAsync(string source, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var database = connection.GetDatabase();
        var key = $"lock:sync:{source}";
        var token = Guid.NewGuid().ToString("N");

        var acquired = await database.StringSetAsync(key, token, ttl, When.NotExists);
        return acquired ? new LockHandle(database, key, token) : null;
    }

    private sealed class LockHandle(IDatabase database, string key, string token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() =>
            await database.ScriptEvaluateAsync(ReleaseScript, new { key = (RedisKey)key, token });
    }
}
