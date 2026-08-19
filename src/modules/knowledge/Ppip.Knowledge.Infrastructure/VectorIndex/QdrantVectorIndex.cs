using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Ppip.Knowledge.Domain.Exceptions;
using Ppip.Knowledge.Domain.Ports;

namespace Ppip.Knowledge.Infrastructure.VectorIndex;

/// <summary>
/// Adaptador real de <see cref="IVectorIndex"/> contra la API REST de Qdrant
/// (ADR-005). Filtro <c>compraAgilId</c> obligatorio en toda búsqueda
/// (ADR-008) — nunca opcional a nivel de este adaptador. El payload jamás
/// incluye el texto del chunk (docs/08-data): se construye explícitamente
/// campo a campo, nunca por serialización automática del DTO de dominio.
/// </summary>
public sealed class QdrantVectorIndex(HttpClient httpClient, IOptions<QdrantOptions> options) : IVectorIndex
{
    public async Task UpsertAsync(VectorPoint point, CancellationToken cancellationToken = default)
    {
        var collection = options.Value.CollectionName;
        var body = new QdrantUpsertRequest([new QdrantPointUpsert(point.PointId, point.Vector, ToPayloadDictionary(point.Payload))]);

        try
        {
            using var response = await httpClient.PutAsJsonAsync($"/collections/{collection}/points?wait=true", body, JsonSerializerOptions.Web, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new RetrievalUnavailableException("No fue posible indexar el vector en Qdrant.", ex);
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] queryVector, string compraAgilId, int topK, CancellationToken cancellationToken = default)
    {
        var collection = options.Value.CollectionName;
        var body = new QdrantSearchRequest(
            queryVector,
            new QdrantFilter([new QdrantFilterCondition("compraAgilId", new QdrantMatch(compraAgilId))]),
            topK,
            WithPayload: true);

        try
        {
            using var response = await httpClient.PostAsJsonAsync($"/collections/{collection}/points/search", body, JsonSerializerOptions.Web, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(JsonSerializerOptions.Web, cancellationToken);
            if (result is null)
            {
                throw new RetrievalUnavailableException("Qdrant devolvió una respuesta vacía en la búsqueda.");
            }

            return [.. result.Result.Where(p => p.Payload is not null).Select(ToSearchResult)];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new RetrievalUnavailableException("Búsqueda vectorial no disponible (Qdrant caído).", ex);
        }
    }

    /// <summary>Crea la colección (docs/10-rag/01: tamaño = dimensión del modelo, distancia coseno) + índice de payload por compraAgilId, si todavía no existen. Idempotente.</summary>
    public static async Task EnsureCollectionAsync(HttpClient httpClient, QdrantOptions options, int dimension, CancellationToken cancellationToken = default)
    {
        var existing = await httpClient.GetAsync($"/collections/{options.CollectionName}", cancellationToken);
        if (existing.IsSuccessStatusCode)
        {
            return;
        }

        var create = await httpClient.PutAsJsonAsync(
            $"/collections/{options.CollectionName}",
            new QdrantCreateCollectionRequest(new QdrantVectorParams(dimension, "Cosine")),
            JsonSerializerOptions.Web,
            cancellationToken);
        create.EnsureSuccessStatusCode();

        var index = await httpClient.PutAsJsonAsync(
            $"/collections/{options.CollectionName}/index",
            new QdrantCreateIndexRequest("compraAgilId", "keyword"),
            JsonSerializerOptions.Web,
            cancellationToken);
        index.EnsureSuccessStatusCode();
    }

    private static Dictionary<string, object?> ToPayloadDictionary(VectorPayload payload) => new()
    {
        ["compraAgilId"] = payload.CompraAgilId,
        ["documentId"] = payload.DocumentId.ToString(),
        ["versionId"] = payload.VersionId.ToString(),
        ["page"] = payload.Page,
        ["section"] = payload.Section,
        ["chunkType"] = payload.ChunkType,
        ["source"] = payload.Source,
        ["hash"] = payload.Hash,
        ["isDemoData"] = payload.IsDemoData,
    };

    private static VectorSearchResult ToSearchResult(QdrantScoredPoint point)
    {
        var p = point.Payload!;
        var payload = new VectorPayload(p.CompraAgilId, p.DocumentId, p.VersionId, p.Page, p.Section, p.ChunkType, p.Source, p.Hash, p.IsDemoData);
        return new VectorSearchResult(point.Id, (float)point.Score, payload);
    }

    private sealed record QdrantUpsertRequest(IReadOnlyList<QdrantPointUpsert> Points);

    private sealed record QdrantPointUpsert(string Id, float[] Vector, Dictionary<string, object?> Payload);

    private sealed record QdrantSearchRequest(float[] Vector, QdrantFilter Filter, int Limit, [property: JsonPropertyName("with_payload")] bool WithPayload);

    private sealed record QdrantFilter(IReadOnlyList<QdrantFilterCondition> Must);

    private sealed record QdrantFilterCondition(string Key, QdrantMatch Match);

    private sealed record QdrantMatch(string Value);

    private sealed record QdrantSearchResponse(IReadOnlyList<QdrantScoredPoint> Result);

    private sealed record QdrantScoredPoint(string Id, double Score, QdrantPayloadDto? Payload);

    private sealed record QdrantPayloadDto(
        [property: JsonPropertyName("compraAgilId")] string CompraAgilId,
        [property: JsonPropertyName("documentId")] Guid DocumentId,
        [property: JsonPropertyName("versionId")] Guid VersionId,
        [property: JsonPropertyName("page")] int Page,
        [property: JsonPropertyName("section")] string? Section,
        [property: JsonPropertyName("chunkType")] string ChunkType,
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("hash")] string Hash,
        [property: JsonPropertyName("isDemoData")] bool IsDemoData);

    private sealed record QdrantCreateCollectionRequest(QdrantVectorParams Vectors);

    private sealed record QdrantVectorParams(int Size, string Distance);

    private sealed record QdrantCreateIndexRequest(
        [property: JsonPropertyName("field_name")] string FieldName,
        [property: JsonPropertyName("field_schema")] string FieldSchema);
}
