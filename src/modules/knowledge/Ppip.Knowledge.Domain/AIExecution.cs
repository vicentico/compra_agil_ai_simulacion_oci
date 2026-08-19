using Ppip.BuildingBlocks.Domain;

namespace Ppip.Knowledge.Domain;

/// <summary>
/// Auditoría de una invocación IA (docs/03-domain/02: entidad compartida entre
/// RAG, FASE 9, y AIAnalysis, FASE 10). UC-005 paso 8: toda consulta RAG deja
/// registro de modelo, prompt, tokens y duración, exista o no evidencia.
/// </summary>
public sealed class AIExecution : Entity<Guid>
{
    public string CompraAgilId { get; }
    public string Model { get; }
    public string PromptVersion { get; }
    public int TokensIn { get; }
    public int TokensOut { get; }
    public long DurationMs { get; }
    public Guid CorrelationId { get; }
    public DateTimeOffset ExecutedAt { get; }

    private AIExecution(
        Guid id,
        string compraAgilId,
        string model,
        string promptVersion,
        int tokensIn,
        int tokensOut,
        long durationMs,
        Guid correlationId,
        DateTimeOffset executedAt)
        : base(id)
    {
        CompraAgilId = compraAgilId;
        Model = model;
        PromptVersion = promptVersion;
        TokensIn = tokensIn;
        TokensOut = tokensOut;
        DurationMs = durationMs;
        CorrelationId = correlationId;
        ExecutedAt = executedAt;
    }

    public static AIExecution Record(
        string compraAgilId,
        string model,
        string promptVersion,
        int tokensIn,
        int tokensOut,
        long durationMs,
        Guid correlationId)
    {
        if (string.IsNullOrWhiteSpace(compraAgilId))
        {
            throw new ArgumentException("El id de la Compra Ágil es obligatorio.", nameof(compraAgilId));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("El modelo usado es obligatorio.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(promptVersion))
        {
            throw new ArgumentException("La versión del prompt es obligatoria.", nameof(promptVersion));
        }

        return new AIExecution(Guid.CreateVersion7(), compraAgilId, model.Trim(), promptVersion.Trim(), tokensIn, tokensOut, durationMs, correlationId, DateTimeOffset.UtcNow);
    }

    /// <summary>Usado por los repositorios para reconstruir desde almacenamiento.</summary>
    public static AIExecution Rehydrate(
        Guid id,
        string compraAgilId,
        string model,
        string promptVersion,
        int tokensIn,
        int tokensOut,
        long durationMs,
        Guid correlationId,
        DateTimeOffset executedAt) =>
        new(id, compraAgilId, model, promptVersion, tokensIn, tokensOut, durationMs, correlationId, executedAt);
}
