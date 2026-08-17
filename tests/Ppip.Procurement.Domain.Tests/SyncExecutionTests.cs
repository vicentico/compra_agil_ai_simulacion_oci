using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Procurement.Domain.Tests;

public class SyncExecutionTests
{
    [Fact]
    public void Start_BeginsRunning()
    {
        var execution = SyncExecution.Start("corr-1");

        Assert.Equal(SyncExecutionStatus.Running, execution.Status);
    }

    [Fact]
    public void Complete_WithoutErrors_IsCompleted()
    {
        var execution = SyncExecution.Start("corr-1");
        execution.RecordCreated();
        execution.RecordUnchanged();

        execution.Complete();

        Assert.Equal(SyncExecutionStatus.Completed, execution.Status);
        Assert.Equal(1, execution.Created);
        Assert.Equal(1, execution.Unchanged);
        Assert.NotNull(execution.FinishedAt);
    }

    [Fact]
    public void Complete_WithErrors_IsCompletedWithErrors()
    {
        var execution = SyncExecution.Start("corr-1");
        execution.RecordError();

        execution.Complete();

        Assert.Equal(SyncExecutionStatus.CompletedWithErrors, execution.Status);
    }

    [Fact]
    public void MarkSkipped_UsedForConcurrentCycle()
    {
        var execution = SyncExecution.Start("corr-1");

        execution.MarkSkipped();

        Assert.Equal(SyncExecutionStatus.Skipped, execution.Status);
    }

    [Fact]
    public void RecordCreated_AfterFinished_Throws()
    {
        var execution = SyncExecution.Start("corr-1");
        execution.Complete();

        Assert.Throws<InvalidOperationException>(execution.RecordCreated);
    }

    [Fact]
    public void Complete_CalledTwice_Throws()
    {
        var execution = SyncExecution.Start("corr-1");
        execution.Complete();

        Assert.Throws<InvalidOperationException>(execution.Complete);
    }
}
