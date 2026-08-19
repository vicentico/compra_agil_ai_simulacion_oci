namespace Ppip.Knowledge.Domain.Ports;

/// <summary>Puerto de <see cref="AIExecution"/> (colección `ai_executions`, docs/08-data/01, índices por correlationId y promptVersion+modelVersion).</summary>
public interface IAIExecutionRepository
{
    Task SaveAsync(AIExecution execution, CancellationToken cancellationToken = default);
}
