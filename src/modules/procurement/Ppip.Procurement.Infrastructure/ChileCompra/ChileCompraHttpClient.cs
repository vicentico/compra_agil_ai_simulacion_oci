using System.Net;
using System.Text.Json;
using Ppip.Procurement.Infrastructure.ChileCompra.Dto;
using Ppip.Procurement.Infrastructure.ChileCompra.Exceptions;

namespace Ppip.Procurement.Infrastructure.ChileCompra;

/// <summary>
/// Adaptador HTTP de <see cref="IChileCompraClient"/>. La resiliencia
/// (retry 1s/5s/25s + circuit breaker, docs/14-reliability/01) se configura
/// en el <see cref="HttpClient"/> vía <c>ServiceCollectionExtensions</c>, no
/// aquí — este tipo solo arma requests, parsea el envelope y mapea errores.
/// </summary>
public sealed class ChileCompraHttpClient(HttpClient httpClient, string ticket) : IChileCompraClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CompraAgilListPayloadDto> ListarAsync(CompraAgilListQuery query, CancellationToken cancellationToken = default)
    {
        var queryString = string.Join('&', query.ToQueryParameters()
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return await SendAsync<CompraAgilListPayloadDto>(
            HttpMethod.Get, $"/v2/compra-agil?{queryString}", codigo: null, cancellationToken);
    }

    public Task<CompraAgilDetailDto> ObtenerDetalleAsync(string codigo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código de la Compra Ágil es obligatorio.", nameof(codigo));
        }

        return SendAsync<CompraAgilDetailDto>(
            HttpMethod.Get, $"/v2/compra-agil/{Uri.EscapeDataString(codigo)}", codigo, cancellationToken);
    }

    private async Task<TPayload> SendAsync<TPayload>(
        HttpMethod method, string requestUri, string? codigo, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("ticket", ticket);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var envelope = Deserialize<ChileCompraEnvelope<TPayload>>(body);
            if (!envelope.IsSuccess || envelope.Payload is null)
            {
                throw new ChileCompraServerException((int)response.StatusCode, "Respuesta 2xx con success != OK — contrato inesperado.");
            }

            return envelope.Payload;
        }

        throw MapError(response, body, codigo);
    }

    private ChileCompraException MapError(HttpResponseMessage response, string body, string? codigo)
    {
        var envelope = TryDeserialize<ChileCompraEnvelope<object>>(body);
        var error = envelope?.Errors?.FirstOrDefault();
        var mensaje = error?.Mensaje ?? $"Error {(int)response.StatusCode} sin cuerpo interpretable.";

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ChileCompraBadRequestException(mensaje, error?.Detalle),
            HttpStatusCode.Unauthorized => new ChileCompraUnauthorizedException(mensaje),
            HttpStatusCode.Forbidden => new ChileCompraForbiddenException(mensaje),
            HttpStatusCode.NotFound => new ChileCompraNotFoundException(codigo ?? "(desconocido)"),
            HttpStatusCode.TooManyRequests => new ChileCompraRateLimitedException(mensaje, response.Headers.RetryAfter?.Delta),
            _ => new ChileCompraServerException((int)response.StatusCode, mensaje),
        };
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new ChileCompraServerException(0, "Respuesta vacía o no deserializable.");

    private static T? TryDeserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
