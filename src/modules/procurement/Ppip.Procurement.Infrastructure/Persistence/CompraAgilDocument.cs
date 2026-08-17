using MongoDB.Bson.Serialization.Attributes;

namespace Ppip.Procurement.Infrastructure.Persistence;

/// <summary>
/// Modelo de persistencia de <c>compras_agiles</c> — deliberadamente
/// separado del agregado de dominio (que tiene constructores privados por
/// diseño, docs/03-domain). <c>Id</c> = código ChileCompra: al usarlo como
/// <c>_id</c>, el índice único de docs/08-data (<c>{codigo:1} unique</c>) lo
/// da gratis Mongo, sin índice adicional.
/// </summary>
internal sealed class CompraAgilDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string InstitutionId { get; set; } = string.Empty;

    public string InstitutionName { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public decimal MontoAmount { get; set; }

    public string MontoCurrency { get; set; } = string.Empty;

    public DateTimeOffset VigenciaStart { get; set; }

    public DateTimeOffset VigenciaEnd { get; set; }

    public string Estado { get; set; } = string.Empty;

    public int Version { get; set; }

    public string RawPayloadHash { get; set; } = string.Empty;

    public DateTimeOffset UltimaActualizacion { get; set; }

    public List<ProductRequirementDocument> Requirements { get; set; } = [];
}

internal sealed class ProductRequirementDocument
{
    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;
}
