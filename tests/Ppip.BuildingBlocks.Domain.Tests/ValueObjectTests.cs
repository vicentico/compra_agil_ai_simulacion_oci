using Ppip.BuildingBlocks.Domain;
using Xunit;

namespace Ppip.BuildingBlocks.Domain.Tests;

public class ValueObjectTests
{
    private sealed class Coordinates(int x, int y) : ValueObject
    {
        public int X { get; } = x;
        public int Y { get; } = y;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return X;
            yield return Y;
        }
    }

    [Fact]
    public void SameComponents_AreEqual()
    {
        Assert.Equal(new Coordinates(1, 2), new Coordinates(1, 2));
    }

    [Fact]
    public void SameComponents_HaveSameHashCode()
    {
        Assert.Equal(new Coordinates(1, 2).GetHashCode(), new Coordinates(1, 2).GetHashCode());
    }

    [Fact]
    public void DifferentComponents_AreNotEqual()
    {
        Assert.NotEqual(new Coordinates(1, 2), new Coordinates(1, 3));
    }
}
