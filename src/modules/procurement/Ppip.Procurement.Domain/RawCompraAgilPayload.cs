using System.Security.Cryptography;
using System.Text;
using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

/// <summary>
/// Payload crudo tal como llegó de ChileCompra — inmutable e imborrable
/// (docs/03-domain/02-domain-model.md). <see cref="ResponseHash"/> es lo que
/// <see cref="SyncPolicy"/> usa para decidir creación/actualización/no-op.
/// </summary>
public sealed class RawCompraAgilPayload : ValueObject
{
    public string Payload { get; }
    public string SourceUrl { get; }
    public DateTimeOffset RetrievedAt { get; }
    public int HttpStatus { get; }
    public string ResponseHash { get; }
    public string ApiVersion { get; }
    public string CorrelationId { get; }

    private RawCompraAgilPayload(
        string payload,
        string sourceUrl,
        DateTimeOffset retrievedAt,
        int httpStatus,
        string responseHash,
        string apiVersion,
        string correlationId)
    {
        Payload = payload;
        SourceUrl = sourceUrl;
        RetrievedAt = retrievedAt;
        HttpStatus = httpStatus;
        ResponseHash = responseHash;
        ApiVersion = apiVersion;
        CorrelationId = correlationId;
    }

    public static RawCompraAgilPayload Capture(
        string payload,
        string sourceUrl,
        DateTimeOffset retrievedAt,
        int httpStatus,
        string apiVersion,
        string correlationId)
    {
        if (string.IsNullOrEmpty(payload))
        {
            throw new ArgumentException("El payload crudo no puede estar vacío.", nameof(payload));
        }

        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            throw new ArgumentException("La URL de origen es obligatoria.", nameof(sourceUrl));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("El correlationId es obligatorio.", nameof(correlationId));
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return new RawCompraAgilPayload(payload, sourceUrl, retrievedAt, httpStatus, hash, apiVersion, correlationId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ResponseHash;
        yield return SourceUrl;
        yield return RetrievedAt;
    }
}
