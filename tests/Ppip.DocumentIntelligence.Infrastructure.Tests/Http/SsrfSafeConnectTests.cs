using Ppip.DocumentIntelligence.Domain.Exceptions;
using Ppip.DocumentIntelligence.Domain.Ports;
using Ppip.DocumentIntelligence.Infrastructure.Http;
using Xunit;

namespace Ppip.DocumentIntelligence.Infrastructure.Tests.Http;

/// <summary>
/// Contra el stack de red real (sin Docker, sin mocks): construye el mismo
/// <see cref="SocketsHttpHandler"/> que <c>AddDocumentDownloader</c> registra
/// en producción y prueba que jamás llega a conectar a un destino privado —
/// exactamente lo que defiende T3 (docs/12-security/02-threat-model.md).
/// </summary>
public class SsrfSafeConnectTests
{
    private static IAttachmentDownloader BuildRealDownloader()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = SsrfSafeConnect.ConnectAsync,
            AllowAutoRedirect = false,
        };
        var httpClient = new HttpClient(handler);
        return new HttpAttachmentDownloader(httpClient);
    }

    [Theory]
    [InlineData("https://127.0.0.1:65535/x.pdf")] // loopback
    [InlineData("https://169.254.169.254/latest/meta-data/")] // link-local / metadata endpoint cloud
    [InlineData("https://10.0.0.1/x.pdf")] // RFC1918
    [InlineData("https://192.168.1.1/x.pdf")] // RFC1918
    public async Task DownloadAsync_PrivateOrLoopbackTarget_ThrowsBlocked(string url)
    {
        var downloader = BuildRealDownloader();

        await Assert.ThrowsAsync<AttachmentBlockedException>(
            () => downloader.DownloadAsync(new Uri(url), maxBytes: 1024, CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_UnresolvableHost_DoesNotThrowBlocked()
    {
        // Un host que no resuelve a nada (ni público ni privado) debe fallar
        // con el error de red normal, NO con AttachmentBlockedException — la
        // distinción importa para el orquestador (reintentar vs. rechazar).
        var downloader = BuildRealDownloader();

        var exception = await Record.ExceptionAsync(
            () => downloader.DownloadAsync(new Uri("https://this-host-does-not-exist.invalid/x.pdf"), maxBytes: 1024, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsNotType<AttachmentBlockedException>(exception);
    }
}
