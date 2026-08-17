using MongoDB.Driver;
using Ppip.Procurement.Domain;
using Ppip.Procurement.Domain.Ports;

namespace Ppip.Procurement.Infrastructure.Persistence;

public sealed class MongoCompraAgilRepository : ICompraAgilRepository
{
    private readonly IMongoCollection<CompraAgilDocument> _collection;

    public MongoCompraAgilRepository(IMongoDatabase database) =>
        _collection = database.GetCollection<CompraAgilDocument>("compras_agiles");

    public async Task<CompraAgil?> FindAsync(CompraAgilId id, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(d => d.Id == id.Value).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task SaveAsync(CompraAgil compra, CancellationToken cancellationToken = default)
    {
        var document = ToDocument(compra);
        await _collection.ReplaceOneAsync(
            d => d.Id == document.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private static CompraAgil ToDomain(CompraAgilDocument document) =>
        CompraAgil.Rehydrate(
            CompraAgilId.From(document.Id),
            InstitutionRef.From(document.InstitutionId, document.InstitutionName),
            document.Titulo,
            Money.From(document.MontoAmount, document.MontoCurrency),
            DateRange.From(document.VigenciaStart, document.VigenciaEnd),
            Enum.Parse<EstadoCompra>(document.Estado),
            document.Version,
            document.RawPayloadHash,
            document.UltimaActualizacion,
            document.Requirements.Select(r => ProductRequirement.Create(r.ProductName, r.Quantity, r.Unit)));

    private static CompraAgilDocument ToDocument(CompraAgil compra) => new()
    {
        Id = compra.Id.Value,
        InstitutionId = compra.Institution.Id,
        InstitutionName = compra.Institution.Name,
        Titulo = compra.Titulo,
        MontoAmount = compra.MontoEstimado.Amount,
        MontoCurrency = compra.MontoEstimado.Currency,
        VigenciaStart = compra.Vigencia.Start,
        VigenciaEnd = compra.Vigencia.End,
        Estado = compra.Estado.ToString(),
        Version = compra.Version,
        RawPayloadHash = compra.RawPayloadHash,
        UltimaActualizacion = compra.UltimaActualizacion,
        Requirements = [.. compra.Requirements.Select(r => new ProductRequirementDocument
        {
            ProductName = r.ProductName,
            Quantity = r.Quantity,
            Unit = r.Unit,
        })],
    };
}
