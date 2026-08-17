using Ppip.BuildingBlocks.Domain;
using Xunit;

namespace Ppip.BuildingBlocks.Domain.Tests;

public class AggregateRootTests
{
    private sealed record SomethingHappened(Guid EventId, DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed class Order(Guid id) : AggregateRoot<Guid>(id)
    {
        public void DoSomething() => Raise(new SomethingHappened(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Raise_AddsEventToDomainEvents()
    {
        var order = new Order(Guid.NewGuid());

        order.DoSomething();

        Assert.Single(order.DomainEvents);
    }

    [Fact]
    public void PullDomainEvents_ReturnsAndClearsEvents()
    {
        var order = new Order(Guid.NewGuid());
        order.DoSomething();
        order.DoSomething();

        var pulled = order.PullDomainEvents();

        Assert.Equal(2, pulled.Count);
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void PullDomainEvents_CalledTwice_SecondCallIsEmpty()
    {
        var order = new Order(Guid.NewGuid());
        order.DoSomething();

        order.PullDomainEvents();
        var secondPull = order.PullDomainEvents();

        Assert.Empty(secondPull);
    }
}
