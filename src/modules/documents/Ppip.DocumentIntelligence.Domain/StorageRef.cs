using Ppip.BuildingBlocks.Domain;

namespace Ppip.DocumentIntelligence.Domain;

/// <summary>Referencia al binario en object storage (ADR-004: bucket `chilecompra`, prefijo `{codigo}/original/`) — MongoDB nunca guarda el binario, solo esta referencia.</summary>
public sealed class StorageRef : ValueObject
{
    public string Bucket { get; }
    public string Key { get; }

    private StorageRef(string bucket, string key)
    {
        Bucket = bucket;
        Key = key;
    }

    public static StorageRef From(string bucket, string key)
    {
        if (string.IsNullOrWhiteSpace(bucket))
        {
            throw new ArgumentException("El bucket es obligatorio.", nameof(bucket));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("La key es obligatoria.", nameof(key));
        }

        return new StorageRef(bucket.Trim(), key.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Bucket;
        yield return Key;
    }

    public override string ToString() => $"{Bucket}/{Key}";
}
