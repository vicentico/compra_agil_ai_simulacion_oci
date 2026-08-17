using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.Procurement.Domain.Tests;

public class SyncCheckpointTests
{
    [Fact]
    public void Initial_HasNoLastSuccessfulSync()
    {
        var checkpoint = SyncCheckpoint.Initial("chilecompra", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        Assert.Null(checkpoint.LastSuccessfulSync);
    }

    [Fact]
    public void Advance_SetsLastSuccessfulSyncAndWindow()
    {
        var checkpoint = SyncCheckpoint.Initial("chilecompra", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        var syncedAt = DateTimeOffset.UtcNow;
        var newStart = syncedAt.AddMinutes(-30);

        checkpoint.Advance(syncedAt, newStart, syncedAt);

        Assert.Equal(syncedAt, checkpoint.LastSuccessfulSync);
        Assert.Equal(newStart, checkpoint.WindowStart);
        Assert.Equal(syncedAt, checkpoint.WindowEnd);
    }

    [Fact]
    public void Advance_InvalidWindow_Throws()
    {
        var checkpoint = SyncCheckpoint.Initial("chilecompra", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => checkpoint.Advance(now, now, now.AddMinutes(-1)));
    }
}
