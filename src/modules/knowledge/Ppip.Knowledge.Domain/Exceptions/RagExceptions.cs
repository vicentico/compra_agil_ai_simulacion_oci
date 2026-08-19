namespace Ppip.Knowledge.Domain.Exceptions;

/// <summary>
/// UC-005 A2 ("Qdrant caído: error explícito con estado de servicio; sin
/// fallback a conocimiento del modelo"): cubre tanto el fallo de
/// <see cref="Ports.IEmbeddingProvider"/> (no se puede vectorizar la
/// pregunta) como el de <see cref="Ports.IVectorIndex"/> — ambos son parte
/// de la misma etapa de recuperación y se exponen como un único caso "qdrant"
/// en el contrato de error (docs/06-api/01-example-rag-query.md).
/// </summary>
public sealed class RetrievalUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>UC-005 A3 ("LLM caído: se muestran los chunks recuperados como resultados de búsqueda sin síntesis") — capturada en Application, nunca llega a ser un error 503 por decisión explícita del caso de uso.</summary>
public sealed class LlmUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
