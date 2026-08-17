using MongoDB.Driver;
using Ppip.BuildingBlocks.Messaging;
using Ppip.Procurement.Infrastructure.Persistence;
using Testcontainers.MongoDb;
using Xunit;

namespace Ppip.Procurement.Infrastructure.Tests.Persistence;

/// <summary>
/// Contra un MongoDB real — agregado en FASE 7 al descubrir (construyendo el
/// módulo de Document Intelligence) que MongoDB.Driver 3.x exige
/// representación explícita para propiedades <c>Guid</c>
/// ("GuidRepresentation is Unspecified" en runtime si falta). Este adaptador
/// (<c>OutboxMessage.Id</c> es <c>Guid</c>) nunca se había probado contra
/// Mongo real en FASE 6 — solo <c>MongoCompraAgilRepositoryTests</c>, cuyo id
/// es <c>string</c>, no disparaba el mismo bug.
/// </summary>
public sealed class MongoOutboxStoreTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private MongoOutboxStore _outbox = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        _outbox = new MongoOutboxStore(client.GetDatabase("procurement_outbox_test"));
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task AppendThenGetPending_RoundTrips()
    {
        var message = new OutboxMessage(Guid.CreateVersion7(), "CompraAgilDetected", "procurement.compra-agil-detected.v1", "{}", DateTimeOffset.UtcNow);

        await _outbox.AppendAsync(message);
        var pending = await _outbox.GetPendingAsync(10);

        var found = Assert.Single(pending);
        Assert.Equal(message.Id, found.Id);
        Assert.False(found.IsPublished);
    }

    [Fact]
    public async Task MarkPublished_RemovesFromPending()
    {
        var message = new OutboxMessage(Guid.CreateVersion7(), "CompraAgilUpdated", "procurement.compra-agil-updated.v1", "{}", DateTimeOffset.UtcNow);
        await _outbox.AppendAsync(message);

        await _outbox.MarkPublishedAsync(message.Id, DateTimeOffset.UtcNow);
        var pending = await _outbox.GetPendingAsync(10);

        Assert.Empty(pending);
    }
}
