namespace Ppip.Knowledge.Domain.Ports;

/// <summary>
/// ADR-007 (simplificado para FASE 9, ver nota en ILlmProvider): puerto hacia
/// el proveedor de embeddings. La implementación concreta (Ollama, mock)
/// vive en Infrastructure — el dominio solo conoce el contrato.
/// </summary>
public interface IEmbeddingProvider
{
    Task<EmbeddingVector> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>Vector resultante más metadatos de procedencia (OQ-03: modelVersion permite reconciliar si el modelo cambia).</summary>
public sealed record EmbeddingVector(float[] Values, string ModelVersion, int Dimension);
