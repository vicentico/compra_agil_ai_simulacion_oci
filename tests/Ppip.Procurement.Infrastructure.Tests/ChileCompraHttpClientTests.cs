using Ppip.Procurement.Infrastructure.ChileCompra;
using Ppip.Procurement.Infrastructure.ChileCompra.Exceptions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Ppip.Procurement.Infrastructure.Tests;

/// <summary>
/// Contract tests contra fixtures reales capturadas de la API Compra Ágil v2
/// (spike de FASE 5, ver fixtures/README.md) — no contra la API en vivo, para
/// no gastar la cuota diaria del ticket en cada corrida de CI.
/// </summary>
public sealed class ChileCompraHttpClientTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    private IChileCompraClient CreateClient() =>
        new ChileCompraHttpClient(new HttpClient { BaseAddress = new Uri(_server.Url!) }, ticket: "test-ticket");

    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", fileName));

    [Fact]
    public async Task ListarAsync_RealFixture_ParsesEnvelopeAndItems()
    {
        _server.Given(Request.Create().WithPath("/v2/compra-agil").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(ReadFixture("real-list-response.json")));

        var result = await CreateClient().ListarAsync(new CompraAgilListQuery { TamanoPagina = 10 });

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(700, result.Paginacion.TotalResultados);
        Assert.Equal(70, result.Paginacion.TotalPaginas);

        var first = result.Items[0];
        Assert.Equal("418-1191-COT26", first.Codigo);
        Assert.Equal("publicada", first.Estado.Codigo);
        Assert.Equal("SERVICIO NACIONAL DE SALUD HOSPITAL DE PUERTO AYSEN", first.Institucion.OrganismoComprador);
        // Hallazgo del spike: fecha_ultimo_cambio (ISO) y fecha_publicacion
        // (formato corto) coexisten en el mismo objeto "fechas" del listado.
        Assert.Equal("2026-08-16T23:05:02.410Z", first.Fechas.FechaUltimoCambio);
        Assert.Equal("2026-08-16 23:02", first.Fechas.FechaPublicacion);
    }

    [Fact]
    public async Task ObtenerDetalleAsync_RealFixture_ParsesPayload()
    {
        _server.Given(Request.Create().WithPath("/v2/compra-agil/418-1191-COT26").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(ReadFixture("real-detail-response.json")));

        var detalle = await CreateClient().ObtenerDetalleAsync("418-1191-COT26");

        Assert.Equal("418-1191-COT26", detalle.Codigo);
        Assert.Equal("publicada", detalle.Estado.Codigo);
        Assert.Null(detalle.IdOrdenCompra);
        Assert.Single(detalle.ProductosSolicitados);
        Assert.Empty(detalle.ProveedoresCotizando);
        Assert.Equal(2_000_000, detalle.Presupuesto.MontoDisponibleClp);
    }

    [Fact]
    public async Task ListarAsync_400Fixture_ThrowsBadRequestWithDetalle()
    {
        // Fixture capturada real: hallazgo del spike (mínimo de tamano_pagina
        // no documentado). La validación del lado del cliente ya lo rechaza
        // antes de llegar a la red — este test cubre el mapeo de errores si
        // la API cambiara sus límites sin que el cliente se actualice.
        _server.Given(Request.Create().WithPath("/v2/compra-agil").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(400).WithHeader("Content-Type", "application/json")
                .WithBody(ReadFixture("error-400-tamano-pagina.json")));

        var ex = await Assert.ThrowsAsync<ChileCompraBadRequestException>(
            () => CreateClient().ListarAsync(new CompraAgilListQuery { TamanoPagina = 10 }));

        Assert.Equal("tamano_pagina debe estar entre 10 y 50", ex.Detalle);
    }

    [Fact]
    public async Task ListarAsync_401Fixture_ThrowsUnauthorized()
    {
        _server.Given(Request.Create().WithPath("/v2/compra-agil").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(401).WithHeader("Content-Type", "application/json")
                .WithBody(ReadFixture("error-401-unauthorized.json")));

        await Assert.ThrowsAsync<ChileCompraUnauthorizedException>(
            () => CreateClient().ListarAsync(new CompraAgilListQuery { TamanoPagina = 10 }));
    }

    [Fact]
    public async Task ObtenerDetalleAsync_404Fixture_ThrowsNotFoundWithCodigo()
    {
        _server.Given(Request.Create().WithPath("/v2/compra-agil/no-existe-999").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404).WithHeader("Content-Type", "application/json")
                .WithBody(ReadFixture("error-404-not-found.json")));

        var ex = await Assert.ThrowsAsync<ChileCompraNotFoundException>(
            () => CreateClient().ObtenerDetalleAsync("no-existe-999"));

        Assert.Equal("no-existe-999", ex.Codigo);
    }

    [Fact]
    public async Task ListarAsync_429Fixture_ThrowsRateLimitedWithRetryAfter()
    {
        _server.Given(Request.Create().WithPath("/v2/compra-agil").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(429)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Retry-After", "3600")
                .WithBody(ReadFixture("error-429-rate-limited.json")));

        var ex = await Assert.ThrowsAsync<ChileCompraRateLimitedException>(
            () => CreateClient().ListarAsync(new CompraAgilListQuery { TamanoPagina = 10 }));

        Assert.Equal(TimeSpan.FromHours(1), ex.RetryAfter);
    }

    [Fact]
    public async Task ListarAsync_503_ThrowsServerException()
    {
        _server.Given(Request.Create().WithPath("/v2/compra-agil").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503).WithHeader("Content-Type", "application/json")
                .WithBody("""{"success":"NOK","trace":null,"payload":null,"errors":[{"codigo":"503","mensaje":"Servicio no disponible.","detalle":null}]}"""));

        var ex = await Assert.ThrowsAsync<ChileCompraServerException>(
            () => CreateClient().ListarAsync(new CompraAgilListQuery { TamanoPagina = 10 }));

        Assert.Equal(503, ex.StatusCode);
    }

    public void Dispose() => _server.Dispose();
}
