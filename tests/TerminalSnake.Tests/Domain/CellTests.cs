using TerminalSnake.Domain;

namespace TerminalSnake.Tests.Domain;

public sealed class CellTests
{
    [Fact]
    public void Addition_combines_coordinates()
    {
        var result = new Cell(2, 3) + new Cell(-1, 4);
        Assert.Equal(new Cell(1, 7), result);
    }

    [Fact]
    public void Subtraction_produces_delta()
    {
        var result = new Cell(5, 5) - new Cell(2, 3);
        Assert.Equal(new Cell(3, 2), result);
    }

    [Fact]
    public void ToString_shows_coordinates()
    {
        Assert.Equal("(4,7)", new Cell(4, 7).ToString());
    }

    [Fact]
    public void Records_with_same_coordinates_are_equal()
    {
        Assert.Equal(new Cell(1, 2), new Cell(1, 2));
        Assert.NotEqual(new Cell(1, 2), new Cell(2, 1));
    }
}
