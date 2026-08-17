using Minio;
using Minio.DataModel.Args;
using Ppip.DocumentIntelligence.Domain;
using Ppip.DocumentIntelligence.Domain.Ports;

namespace Ppip.DocumentIntelligence.Infrastructure.Storage;

/// <summary>Adaptador real de <see cref="IObjectStorage"/> (ADR-004) — crea el bucket si no existe (primera escritura del POC).</summary>
public sealed class MinioObjectStorage(IMinioClient client) : IObjectStorage
{
    public async Task<StorageRef> SaveAsync(string bucket, string key, byte[] content, string? contentType, CancellationToken cancellationToken = default)
    {
        var bucketExists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), cancellationToken);
        if (!bucketExists)
        {
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);
        }

        using var stream = new MemoryStream(content);
        var putArgs = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(key)
            .WithStreamData(stream)
            .WithObjectSize(content.LongLength)
            .WithContentType(contentType ?? "application/octet-stream");

        await client.PutObjectAsync(putArgs, cancellationToken);
        return StorageRef.From(bucket, key);
    }
}
