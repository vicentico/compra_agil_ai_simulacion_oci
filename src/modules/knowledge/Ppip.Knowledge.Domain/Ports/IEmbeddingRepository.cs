namespace Ppip.Knowledge.Domain.Ports;

/// <summary>Puerto de <see cref="Embedding"/> (colección `embeddings`, docs/08-data/01: referencias, no vectores).</summary>
public interface IEmbeddingRepository
{
    Task SaveAsync(Embedding embedding, CancellationToken cancellationToken = default);
}
