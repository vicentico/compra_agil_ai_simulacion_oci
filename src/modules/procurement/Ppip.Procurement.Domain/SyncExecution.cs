using Ppip.BuildingBlocks.Domain;

namespace Ppip.Procurement.Domain;

public enum SyncExecutionStatus
{
    Running,
    Completed,
    CompletedWithErrors,
    Aborted,
    Skipped,
}

/// <summary>
/// Ejecución de un ciclo de sincronización: contadores, errores, duración,
/// correlationId (UC-001 paso 9, docs/03-domain/02-domain-model.md).
/// </summary>
public sealed class SyncExecution : AggregateRoot<Guid>
{
    public string CorrelationId { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public int Created { get; private set; }
    public int Updated { get; private set; }
    public int Unchanged { get; private set; }
    public int Errors { get; private set; }
    public SyncExecutionStatus Status { get; private set; }

    private SyncExecution(Guid id, string correlationId, DateTimeOffset startedAt) : base(id)
    {
        CorrelationId = correlationId;
        StartedAt = startedAt;
        Status = SyncExecutionStatus.Running;
    }

    public static SyncExecution Start(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("El correlationId es obligatorio.", nameof(correlationId));
        }

        return new SyncExecution(Guid.CreateVersion7(), correlationId, DateTimeOffset.UtcNow);
    }

    public void RecordCreated() => EnsureRunning(() => Created++);

    public void RecordUpdated() => EnsureRunning(() => Updated++);

    public void RecordUnchanged() => EnsureRunning(() => Unchanged++);

    public void RecordError() => EnsureRunning(() => Errors++);

    /// <summary>UC-001 A5: segundo ciclo concurrente termina de inmediato como skipped.</summary>
    public void MarkSkipped() => Finish(SyncExecutionStatus.Skipped);

    public void Abort() => Finish(SyncExecutionStatus.Aborted);

    public void Complete() => Finish(Errors > 0 ? SyncExecutionStatus.CompletedWithErrors : SyncExecutionStatus.Completed);

    private void EnsureRunning(Action action)
    {
        if (Status != SyncExecutionStatus.Running)
        {
            throw new InvalidOperationException($"La ejecución {Id} ya finalizó ({Status}).");
        }

        action();
    }

    private void Finish(SyncExecutionStatus status)
    {
        if (Status != SyncExecutionStatus.Running)
        {
            throw new InvalidOperationException($"La ejecución {Id} ya finalizó ({Status}).");
        }

        Status = status;
        FinishedAt = DateTimeOffset.UtcNow;
    }
}
