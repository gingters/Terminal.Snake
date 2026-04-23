using TerminalSnake.Domain;

namespace TerminalSnake.Tests.Domain;

public sealed class SnakeTests
{
    [Fact]
    public void Head_is_first_segment()
    {
        var snake = new Snake(new[] { new Cell(2, 2), new Cell(1, 2), new Cell(0, 2) }, SnakeColor.Red);
        Assert.Equal(new Cell(2, 2), snake.Head);
    }

    [Theory]
    [InlineData(2, 2, 1, 2, Direction.Right)]
    [InlineData(2, 2, 3, 2, Direction.Left)]
    [InlineData(2, 2, 2, 3, Direction.Up)]
    [InlineData(2, 2, 2, 1, Direction.Down)]
    public void Direction_derived_from_head_and_second_segment(
        int hx, int hy, int sx, int sy, Direction expected)
    {
        var snake = new Snake(
            new[] { new Cell(hx, hy), new Cell(sx, sy) }, SnakeColor.Cyan);
        Assert.Equal(expected, snake.Direction);
    }

    [Theory]
    [InlineData(Direction.Right, 2, 3, 9, 3)]
    [InlineData(Direction.Left, 2, 3, 0, 3)]
    [InlineData(Direction.Up, 2, 3, 2, 0)]
    [InlineData(Direction.Down, 2, 3, 2, 9)]
    public void ExitCell_projects_head_to_border_in_direction(
        Direction direction, int hx, int hy, int ex, int ey)
    {
        var second = new Cell(hx, hy) + direction.Opposite().Delta();
        var snake = new Snake(new[] { new Cell(hx, hy), second }, SnakeColor.Green);
        Assert.Equal(new Cell(ex, ey), snake.ExitCell(10));
    }

    [Fact]
    public void ExitCell_rejects_non_positive_board_size()
    {
        var snake = new Snake(new[] { new Cell(1, 1), new Cell(0, 1) }, SnakeColor.Green);
        Assert.Throws<ArgumentOutOfRangeException>(() => snake.ExitCell(0));
    }

    [Fact]
    public void Constructor_rejects_single_segment()
    {
        Assert.Throws<ArgumentException>(() =>
            new Snake(new[] { new Cell(0, 0) }, SnakeColor.Red));
    }

    [Fact]
    public void Constructor_rejects_self_intersection()
    {
        Assert.Throws<ArgumentException>(() =>
            new Snake(new[] { new Cell(0, 0), new Cell(1, 0), new Cell(0, 0) }, SnakeColor.Red));
    }

    [Fact]
    public void Constructor_rejects_non_orthogonal_segments()
    {
        Assert.Throws<ArgumentException>(() =>
            new Snake(new[] { new Cell(0, 0), new Cell(2, 0) }, SnakeColor.Red));
    }

    [Fact]
    public void WithSegments_keeps_color_and_applies_new_path()
    {
        var original = new Snake(new[] { new Cell(0, 0), new Cell(1, 0) }, SnakeColor.Magenta);
        var replaced = original.WithSegments(new[] { new Cell(2, 2), new Cell(1, 2), new Cell(0, 2) });
        Assert.Equal(SnakeColor.Magenta, replaced.Color);
        Assert.Equal(3, replaced.Segments.Length);
        Assert.Equal(new Cell(2, 2), replaced.Head);
    }
}
