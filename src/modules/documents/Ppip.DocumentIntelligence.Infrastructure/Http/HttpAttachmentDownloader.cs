using Ppip.DocumentIntelligence.Domain.Exceptions;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Http;

/// <summary>
/// Adaptador real de <see cref="IAttachmentDownloader"/>: nunca buferea más
/// de <c>maxBytes</c> (corta el stream apenas se excede, antes de terminar
/// de descargar un binario sobredimensionado). La resiliencia (retry/circuit
/// breaker/timeout) y la validación anti-SSRF viven en el
/// <see cref="System.Net.Http.SocketsHttpHandler"/> configurado en
/// <see cref="ServiceCollectionExtensions"/> — este adaptador no las conoce.
/// </summary>
public sealed class HttpAttachmentDownloader(HttpClient httpClient) : IAttachmentDownloader
{
    private const int BufferSize = 81_920;

    public async Task<DownloadedAttachment> DownloadAsync(Uri url, long maxBytes, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex) when (ConnectExceptionUnwrapper.Unwrap(ex) is { } unwrapped)
        {
            // SocketsHttpHandler envuelve cualquier excepción del
            // ConnectCallback (incluida AttachmentBlockedException, lanzada
            // por SsrfSafeConnect) en HttpRequestException — sin desenvolver
            // acá, el caller nunca ve el tipo real y trata un bloqueo SSRF
            // como una falla de red reintentable, que es exactamente lo que
            // NO debe pasar (hallazgo real: los tests contra la pila de red
            // real, no mocks, lo expusieron).
            throw unwrapped;
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is { } declaredLength && declaredLength > maxBytes)
            {
                throw new AttachmentTooLargeException($"El servidor declara {declaredLength} bytes, supera el máximo permitido ({maxBytes}).");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[BufferSize];
            long total = 0;
            int read;
            while ((read = await responseStream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new AttachmentTooLargeException($"El binario supera el máximo permitido ({maxBytes} bytes) durante la descarga.");
                }

                buffer.Write(chunk, 0, read);
            }

            return new DownloadedAttachment(buffer.ToArray(), response.Content.Headers.ContentType?.MediaType);
        }
    }
}

/// <summary>Compartido con <see cref="ServiceCollectionExtensions"/> (Polly también necesita ver a través del wrapper para no reintentar un bloqueo SSRF).</summary>
internal static class ConnectExceptionUnwrapper
{
    public static Exception? Unwrap(HttpRequestException ex) =>
        ex.InnerException is AttachmentBlockedException or AttachmentTooLargeException ? ex.InnerException : null;
}
