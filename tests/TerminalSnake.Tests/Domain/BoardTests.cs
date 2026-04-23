using TerminalSnake.Domain;

namespace TerminalSnake.Tests.Domain;

public sealed class BoardTests
{
    private static Snake SnakeAt(SnakeColor color, params (int X, int Y)[] cells)
    {
        var segments = cells.Select(c => new Cell(c.X, c.Y)).ToArray();
        return new Snake(segments, color);
    }

    [Fact]
    public void IsInside_returns_true_for_cells_in_bounds()
    {
        var board = new Board(5, new[] { SnakeAt(SnakeColor.Red, (0, 0), (1, 0)) });
        Assert.True(board.IsInside(new Cell(0, 0)));
        Assert.True(board.IsInside(new Cell(4, 4)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    public void IsInside_returns_false_outside_bounds(int x, int y)
    {
        var board = new Board(5, new[] { SnakeAt(SnakeColor.Red, (0, 0), (1, 0)) });
        Assert.False(board.IsInside(new Cell(x, y)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 0)]
    [InlineData(0, 4)]
    [InlineData(4, 4)]
    [InlineData(2, 0)]
    public void IsBorder_detects_border_cells(int x, int y)
    {
        var board = new Board(5, new[] { SnakeAt(SnakeColor.Red, (0, 0), (1, 0)) });
        Assert.True(board.IsBorder(new Cell(x, y)));
    }

    [Fact]
    public void IsBorder_rejects_interior_cells()
    {
        var board = new Board(5, new[] { SnakeAt(SnakeColor.Red, (0, 0), (1, 0)) });
        Assert.False(board.IsBorder(new Cell(2, 2)));
    }

    [Fact]
    public void OccupyingSnake_returns_index_for_snake_cells()
    {
        var snake0 = SnakeAt(SnakeColor.Red, (0, 0), (1, 0));
        var snake1 = SnakeAt(SnakeColor.Cyan, (2, 2), (2, 3));
        var board = new Board(5, new[] { snake0, snake1 });
        Assert.Equal(0, board.OccupyingSnake(new Cell(0, 0)));
        Assert.Equal(1, board.OccupyingSnake(new Cell(2, 3)));
    }

    [Fact]
    public void OccupyingSnake_returns_null_for_empty_cells()
    {
        var board = new Board(5, new[] { SnakeAt(SnakeColor.Red, (0, 0), (1, 0)) });
        Assert.Null(board.OccupyingSnake(new Cell(3, 3)));
        Assert.True(board.IsEmpty(new Cell(3, 3)));
    }

    [Fact]
    public void Constructor_rejects_size_below_three()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Board(2, new[] { SnakeAt(SnakeColor.Red, (0, 0), (1, 0)) }));
    }

    [Fact]
    public void Constructor_rejects_snake_out_of_bounds()
    {
        Assert.Throws<ArgumentException>(() =>
            new Board(3, new[] { SnakeAt(SnakeColor.Red, (0, 0), (1, 0), (2, 0), (3, 0)) }));
    }

    [Fact]
    public void Constructor_rejects_overlapping_snakes()
    {
        var a = SnakeAt(SnakeColor.Red, (0, 0), (1, 0));
        var b = SnakeAt(SnakeColor.Cyan, (1, 0), (2, 0));
        Assert.Throws<ArgumentException>(() => new Board(5, new[] { a, b }));
    }

    [Fact]
    public void WithSnakes_produces_new_board_with_same_size()
    {
        var original = new Board(5, new[] { SnakeAt(SnakeColor.Red, (0, 0), (1, 0)) });
        var replaced = original.WithSnakes(new[] { SnakeAt(SnakeColor.Cyan, (2, 2), (2, 3)) });
        Assert.Equal(5, replaced.Size);
        Assert.Single(replaced.Snakes);
        Assert.Equal(SnakeColor.Cyan, replaced.Snakes[0].Color);
    }
}
