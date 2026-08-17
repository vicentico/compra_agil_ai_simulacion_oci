using MongoDB.Driver;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Infrastructure.Persistence;
using Testcontainers.MongoDb;
using Xunit;

namespace Ppip.Procurement.Infrastructure.Tests.Persistence;

/// <summary>
/// Contra un MongoDB real (Testcontainers) — no un doble: valida que
/// <c>CompraAgil.Rehydrate</c> reconstruye exactamente lo que
/// <c>SaveAsync</c> guardó, incluyendo el upsert (segunda escritura sobre el
/// mismo código no crea un segundo documento — NFR-001).
/// </summary>
public sealed class MongoCompraAgilRepositoryTests : IAsyncLifetime
{
    // Misma imagen que infrastructure/docker/docker-compose.yml (servicio mongodb) — consistencia con el entorno real.
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private MongoCompraAgilRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var client = new MongoClient(_container.GetConnectionString());
        _repository = new MongoCompraAgilRepository(client.GetDatabase("procurement_test"));
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    private static CompraAgil NewCompra(string codigo = "418-1191-COT26", string hash = "hash-v1") =>
        CompraAgil.Detect(
            CompraAgilId.From(codigo),
            InstitutionRef.From("61.602.279-2", "Hospital de Ejemplo"),
            "Compra de notebooks",
            Money.From(1_000_000, "CLP"),
            DateRange.From(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(2)),
            hash,
            [ProductRequirement.Create("Notebook", 5, "unidad")],
            "corr-1");

    [Fact]
    public async Task SaveThenFind_RoundTripsExactState()
    {
        var compra = NewCompra();
        compra.Cerrar();

        await _repository.SaveAsync(compra);
        var found = await _repository.FindAsync(compra.Id);

        Assert.NotNull(found);
        Assert.Equal(compra.Id, found!.Id);
        Assert.Equal(EstadoCompra.Cerrada, found.Estado);
        Assert.Equal(compra.Titulo, found.Titulo);
        Assert.Equal(compra.MontoEstimado, found.MontoEstimado);
        Assert.Equal(compra.Vigencia, found.Vigencia);
        Assert.Equal(compra.RawPayloadHash, found.RawPayloadHash);
        Assert.Single(found.Requirements);
        Assert.Empty(found.DomainEvents);
    }

    [Fact]
    public async Task Find_UnknownId_ReturnsNull()
    {
        var found = await _repository.FindAsync(CompraAgilId.From("no-existe"));

        Assert.Null(found);
    }

    [Fact]
    public async Task SaveTwiceWithSameCodigo_UpsertsRatherThanDuplicating()
    {
        var compra = NewCompra(hash: "hash-v1");
        await _repository.SaveAsync(compra);

        compra.ApplyUpdate("Compra de notebooks y monitores", compra.MontoEstimado, compra.Vigencia, "hash-v2", compra.Requirements, "corr-2");
        await _repository.SaveAsync(compra);

        var found = await _repository.FindAsync(compra.Id);
        Assert.Equal(2, found!.Version);
        Assert.Equal("hash-v2", found.RawPayloadHash);
    }
}
