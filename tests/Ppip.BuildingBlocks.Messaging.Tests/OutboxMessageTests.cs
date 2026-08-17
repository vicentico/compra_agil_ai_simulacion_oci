using Ppip.BuildingBlocks.Messaging;
using Xunit;

namespace Ppip.BuildingBlocks.Messaging.Tests;

public class OutboxMessageTests
{
    private static OutboxMessage CreateMessage() =>
        new(Guid.NewGuid(), "CompraAgilDetected", "procurement.compra-agil-detected.v1", "{}", DateTimeOffset.UtcNow);

    [Fact]
    public void NewMessage_IsNotPublished()
    {
        Assert.False(CreateMessage().IsPublished);
    }

    [Fact]
    public void MarkPublished_SetsIsPublished()
    {
        var message = CreateMessage();

        message.MarkPublished(DateTimeOffset.UtcNow);

        Assert.True(message.IsPublished);
    }

    [Fact]
    public void MarkPublished_Twice_Throws()
    {
        var message = CreateMessage();
        message.MarkPublished(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => message.MarkPublished(DateTimeOffset.UtcNow));
    }
}
