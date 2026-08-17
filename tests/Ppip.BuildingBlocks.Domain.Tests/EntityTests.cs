using Ppip.BuildingBlocks.Domain;
using Xunit;

namespace Ppip.BuildingBlocks.Domain.Tests;

public class EntityTests
{
    private sealed class Widget(Guid id) : Entity<Guid>(id);

    private sealed class Gadget(Guid id) : Entity<Guid>(id);

    [Fact]
    public void SameTypeAndId_AreEqual()
    {
        var id = Guid.NewGuid();
        Assert.Equal(new Widget(id), new Widget(id));
    }

    [Fact]
    public void SameId_DifferentType_AreNotEqual()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual<Entity<Guid>>(new Widget(id), new Gadget(id));
    }

    [Fact]
    public void DifferentId_SameType_AreNotEqual()
    {
        Assert.NotEqual(new Widget(Guid.NewGuid()), new Widget(Guid.NewGuid()));
    }
}
