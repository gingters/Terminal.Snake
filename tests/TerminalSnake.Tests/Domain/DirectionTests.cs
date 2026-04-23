using TerminalSnake.Domain;

namespace TerminalSnake.Tests.Domain;

public sealed class DirectionTests
{
    [Theory]
    [InlineData(Direction.Up, 0, -1)]
    [InlineData(Direction.Down, 0, 1)]
    [InlineData(Direction.Left, -1, 0)]
    [InlineData(Direction.Right, 1, 0)]
    public void Delta_returns_unit_vector_for_direction(Direction direction, int expectedX, int expectedY)
    {
        Assert.Equal(new Cell(expectedX, expectedY), direction.Delta());
    }

    [Theory]
    [InlineData(Direction.Up, Direction.Down)]
    [InlineData(Direction.Down, Direction.Up)]
    [InlineData(Direction.Left, Direction.Right)]
    [InlineData(Direction.Right, Direction.Left)]
    public void Opposite_flips_direction(Direction direction, Direction expected)
    {
        Assert.Equal(expected, direction.Opposite());
    }

    [Theory]
    [InlineData(0, 0, 1, 0, Direction.Right)]
    [InlineData(0, 0, -1, 0, Direction.Left)]
    [InlineData(0, 0, 0, 1, Direction.Down)]
    [InlineData(0, 0, 0, -1, Direction.Up)]
    public void FromDelta_recognizes_orthogonal_neighbors(
        int fx, int fy, int tx, int ty, Direction expected)
    {
        Assert.Equal(expected, DirectionExtensions.FromDelta(new Cell(fx, fy), new Cell(tx, ty)));
    }

    [Fact]
    public void FromDelta_rejects_non_neighbors()
    {
        Assert.Throws<ArgumentException>(() =>
            DirectionExtensions.FromDelta(new Cell(0, 0), new Cell(2, 0)));
    }

    [Fact]
    public void FromDelta_rejects_diagonal()
    {
        Assert.Throws<ArgumentException>(() =>
            DirectionExtensions.FromDelta(new Cell(0, 0), new Cell(1, 1)));
    }

    [Fact]
    public void Delta_throws_on_invalid_enum_value()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Direction)999).Delta());
    }

    [Fact]
    public void Opposite_throws_on_invalid_enum_value()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((Direction)999).Opposite());
    }
}
