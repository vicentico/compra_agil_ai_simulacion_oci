using Minio;
using Minio.DataModel.Args;
using Ppip.DocumentIntelligence.Infrastructure.Storage;
using Testcontainers.Minio;
using Xunit;

namespace Ppip.DocumentIntelligence.Infrastructure.Tests.Storage;

/// <summary>Contra un MinIO real — valida que el bucket se crea solo y que el binario guardado es exactamente el que se leyó de vuelta (ADR-004).</summary>
public sealed class MinioObjectStorageTests : IAsyncLifetime
{
    private readonly MinioContainer _container = new MinioBuilder("minio/minio:latest").Build();
    private IMinioClient _client = null!;
    private MinioObjectStorage _storage = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var endpoint = _container.GetConnectionString().Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase);
        _client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(_container.GetAccessKey(), _container.GetSecretKey())
            .WithSSL(false)
            .Build();
        _storage = new MinioObjectStorage(_client);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task SaveAsync_CreatesBucketAndStoresExactBytes()
    {
        var content = "%PDF-1.7 contenido de prueba"u8.ToArray();

        var storageRef = await _storage.SaveAsync("chilecompra", "418-1191-COT26/original/bases.pdf", content, "application/pdf");

        Assert.Equal("chilecompra", storageRef.Bucket);
        Assert.Equal("418-1191-COT26/original/bases.pdf", storageRef.Key);

        using var memory = new MemoryStream();
        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(storageRef.Bucket)
            .WithObject(storageRef.Key)
            .WithCallbackStream(stream => stream.CopyTo(memory)));

        Assert.Equal(content, memory.ToArray());
    }

    [Fact]
    public async Task SaveAsync_SecondObjectSameBucket_DoesNotFailOnExistingBucket()
    {
        await _storage.SaveAsync("chilecompra", "a/x.pdf", "%PDF-1"u8.ToArray(), "application/pdf");

        var storageRef = await _storage.SaveAsync("chilecompra", "b/y.pdf", "%PDF-2"u8.ToArray(), "application/pdf");

        Assert.Equal("b/y.pdf", storageRef.Key);
    }
}
