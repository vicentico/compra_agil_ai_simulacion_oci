namespace Ppip.DocumentIntelligence.Domain.Ports;

/// <summary>Puerto del agregado <see cref="Document"/> (NFR-013). Adaptador Mongo real en FASE 7 (colecciones `documents` + `document_versions`, docs/08-data/01).</summary>
public interface IDocumentRepository
{
    Task<Document?> FindAsync(DocumentId id, CancellationToken cancellationToken = default);

    /// <summary>Búsqueda por identidad natural (compraAgilId + sourceUrl) — evita crear un <see cref="Document"/> duplicado para el mismo adjunto en un reintento del sync.</summary>
    Task<Document?> FindByCompraAndUrlAsync(string compraAgilId, string sourceUrl, CancellationToken cancellationToken = default);

    Task SaveAsync(Document document, CancellationToken cancellationToken = default);
}
