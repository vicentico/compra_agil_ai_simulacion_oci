using System.Text.RegularExpressions;
using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>Hash SHA-256 del binario descargado (FR-011) — 64 caracteres hex en minúsculas.</summary>
public sealed partial class Sha256Hash : ValueObject
{
    public string Value { get; }

    private Sha256Hash(string value) => Value = value;

    public static Sha256Hash From(string hexHash)
    {
        if (string.IsNullOrWhiteSpace(hexHash))
        {
            throw new ArgumentException("El hash SHA-256 es obligatorio.", nameof(hexHash));
        }

        var normalized = hexHash.Trim().ToLowerInvariant();
        if (!HexPattern().IsMatch(normalized))
        {
            throw new ArgumentException("El hash SHA-256 debe ser 64 caracteres hexadecimales.", nameof(hexHash));
        }

        return new Sha256Hash(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex HexPattern();
}
