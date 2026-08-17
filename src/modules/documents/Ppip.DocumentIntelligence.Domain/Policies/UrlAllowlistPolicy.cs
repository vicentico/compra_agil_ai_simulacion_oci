using System.Net;

namespace Ppip.DocumentIntelligence.Domain.Policies;

/// <summary>
/// Primera línea de defensa anti-SSRF (T3, docs/12-security/02-threat-model.md):
/// solo HTTPS, solo hosts que matchean el allowlist configurado, nunca un IP
/// literal (los dominios reales de descarga son siempre nombres). Puro — no
/// resuelve DNS; la revalidación de la IP resuelta al momento de conectar
/// (para no ser vulnerable a DNS rebinding) es responsabilidad de
/// <c>IAttachmentDownloader</c> en Infrastructure.
/// </summary>
public static class UrlAllowlistPolicy
{
    public static bool IsAllowed(Uri url, IReadOnlyList<string> allowedHostPatterns)
    {
        if (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IPAddress.TryParse(url.Host, out _))
        {
            return false;
        }

        foreach (var pattern in allowedHostPatterns)
        {
            if (MatchesPattern(url.Host, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesPattern(string host, string pattern)
    {
        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            var suffix = pattern[1..];
            return host.Length > suffix.Length && host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
