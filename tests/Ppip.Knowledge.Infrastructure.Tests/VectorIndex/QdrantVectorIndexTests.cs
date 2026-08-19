using Microsoft.Extensions.Options;
using Ppip.Knowledge.Domain.Ports;
using Ppip.Knowledge.Infrastructure.VectorIndex;
using Testcontainers.Qdrant;
using Xunit;

namespace Ppip.Knowledge.Infrastructure.Tests.VectorIndex;

/// <summary>
/// Contra un Qdrant real (Testcontainers) — a diferencia de Ollama (validado
/// solo vía forma del adaptador, sin modelo real descargado en esta sesión),
/// Qdrant arranca rápido y sin dependencias pesadas, por lo que SÍ se valida
/// end-to-end: creación de colección, upsert, búsqueda filtrada por
/// compraAgilId (ADR-008).
/// </summary>
public sealed class QdrantVectorIndexTests : IAsyncLifetime
{
    private readonly QdrantContainer _container = new QdrantBuilder("qdrant/qdrant:v1.12.4").Build();
    private QdrantVectorIndex _index = null!;
    private HttpClient _httpClient = null!;
    private const string CollectionName = "chunks_v1_test";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var options = new QdrantOptions { Endpoint = _container.GetConnectionString(), CollectionName = CollectionName };
        _httpClient = new HttpClient { BaseAddress = new Uri(options.Endpoint) };
        await Ppip.Knowledge.Infrastructure.VectorIndex.QdrantVectorIndex.EnsureCollectionAsync(_httpClient, options, dimension: 4);
        _index = new QdrantVectorIndex(_httpClient, Options.Create(options));
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        await _container.DisposeAsync();
    }

    private static VectorPayload Payload(string compraAgilId) =>
        new(compraAgilId, Guid.CreateVersion7(), Guid.CreateVersion7(), 7, "1. Plazo de entrega", "Paragraph", "chilecompra", new string('a', 64), IsDemoData: false);

    [Fact]
    public async Task UpsertThenSearch_FindsPointFilteredByCompraAgilId()
    {
        var pointId = Guid.CreateVersion7().ToString();
        await _index.UpsertAsync(new VectorPoint(pointId, [1f, 0f, 0f, 0f], Payload("418-1191-COT26")));

        var results = await _index.SearchAsync([1f, 0f, 0f, 0f], "418-1191-COT26", topK: 8);

        var result = Assert.Single(results);
        Assert.Equal(pointId, result.PointId);
        Assert.Equal("418-1191-COT26", result.Payload.CompraAgilId);
        Assert.True(result.Score > 0.9f);
    }

    [Fact]
    public async Task Search_DifferentCompraAgilId_NeverReturnsOtherCompraPoints()
    {
        await _index.UpsertAsync(new VectorPoint(Guid.CreateVersion7().ToString(), [1f, 0f, 0f, 0f], Payload("418-1191-COT26")));

        // ADR-008: el filtro server-side es obligatorio — una compra distinta jamás ve evidencia ajena, aunque el vector sea idéntico.
        var results = await _index.SearchAsync([1f, 0f, 0f, 0f], "999-9999-COT26", topK: 8);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_NoPointsIndexed_ReturnsEmpty()
    {
        var results = await _index.SearchAsync([1f, 0f, 0f, 0f], "418-1191-COT26", topK: 8);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Upsert_SamePointId_ReplacesVectorAndPayload()
    {
        var pointId = Guid.CreateVersion7().ToString();
        await _index.UpsertAsync(new VectorPoint(pointId, [1f, 0f, 0f, 0f], Payload("418-1191-COT26")));
        await _index.UpsertAsync(new VectorPoint(pointId, [0f, 1f, 0f, 0f], Payload("418-1191-COT26")));

        var results = await _index.SearchAsync([0f, 1f, 0f, 0f], "418-1191-COT26", topK: 8);

        var result = Assert.Single(results);
        Assert.True(result.Score > 0.9f);
    }
}
