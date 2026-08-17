namespace Ppip.DocumentIntelligence.Domain.Policies;

/// <summary>
/// Verifica el binario real, no lo que declara el header Content-Type (docs/12-security/01:
/// "validación content-type + magic bytes") — un servidor puede mentir sobre
/// el tipo. ASM-02: los adjuntos son mayoritariamente PDF; otros formatos se
/// suman cuando FASE 7+ los necesite (no antes, evita validadores especulativos).
/// </summary>
public static class PdfMagicBytes
{
    private static readonly byte[] Signature = "%PDF-"u8.ToArray();

    public static bool Matches(ReadOnlySpan<byte> header) =>
        header.Length >= Signature.Length && header[..Signature.Length].SequenceEqual(Signature);
}
