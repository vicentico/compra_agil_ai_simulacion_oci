namespace Ppip.DocumentIntelligence.Domain.Ports;

/// <summary>
/// Puerto de descarga (NFR-013): la aplicación depende de esto, nunca de
/// <c>HttpClient</c> directamente. El adaptador real (FASE 7,
/// <c>Ppip.DocumentIntelligence.Infrastructure</c>) revalida la IP resuelta al conectar
/// (anti-SSRF/DNS-rebinding, T3) y aplica <paramref name="maxBytes"/> en
/// streaming — nunca buferea un binario sobredimensionado completo.
/// </summary>
public interface IAttachmentDownloader
{
    Task<DownloadedAttachment> DownloadAsync(Uri url, long maxBytes, CancellationToken cancellationToken = default);
}

/// <summary><paramref name="ContentType"/> es lo que declaró el servidor — no confiable por sí solo, ver <c>Policies.PdfMagicBytes</c>.</summary>
public sealed record DownloadedAttachment(byte[] Content, string? ContentType);
