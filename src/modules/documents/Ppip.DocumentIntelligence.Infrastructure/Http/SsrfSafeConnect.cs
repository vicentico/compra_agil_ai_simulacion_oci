using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Ppip.DocumentIntelligence.Domain.Exceptions;

namespace Ppip.DocumentIntelligence.Infrastructure.Http;

/// <summary>
/// <c>ConnectCallback</c> de <see cref="System.Net.Http.SocketsHttpHandler"/>
/// (mismo mecanismo que <c>Ppip.BuildingBlocks.Security</c> usó en FASE 3
/// para forzar la conexión física de JwtBearer): resuelve DNS y valida la IP
/// resultante en el mismo paso que conecta, así no es vulnerable a DNS
/// rebinding (T3, docs/12-security/02-threat-model.md — un allowlist de
/// hostname por sí solo no protege si el DNS cambia entre la validación y la
/// conexión real).
/// </summary>
/// <summary>Público a propósito: los tests de infraestructura arman el mismo <see cref="System.Net.Http.SocketsHttpHandler"/> exacto de producción para probar la defensa anti-SSRF contra la pila de red real.</summary>
public static class SsrfSafeConnect
{
    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        var safeAddress = Array.Find(addresses, IsPublicAddress)
            ?? throw new AttachmentBlockedException($"'{host}' no resolvió a ninguna IP pública permitida (anti-SSRF/DNS-rebinding).");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(safeAddress, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return false;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return true;
        }

        var b = address.GetAddressBytes();
        return b[0] switch
        {
            10 => false, // 10.0.0.0/8
            127 => false, // 127.0.0.0/8
            0 => false, // 0.0.0.0/8
            169 when b[1] == 254 => false, // 169.254.0.0/16 (link-local, incluye endpoints de metadata cloud)
            172 when b[1] is >= 16 and <= 31 => false, // 172.16.0.0/12
            192 when b[1] == 168 => false, // 192.168.0.0/16
            _ => true,
        };
    }
}
