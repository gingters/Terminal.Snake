using TerminalSnake.Domain;
using TerminalSnake.Game;

namespace TerminalSnake.Tests.Game;

public sealed class SnakeSelectionTests
{
    private static Snake Snake(SnakeColor color, params (int X, int Y)[] cells) =>
        new(cells.Select(c => new Cell(c.X, c.Y)), color);

    [Fact]
    public void Rejects_null_snakes()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SnakeSelection.FindNearestInDirection(null!, new Cell(0, 0), Direction.Right, null));
    }

    [Fact]
    public void Picks_the_snake_directly_right_of_origin()
    {
        var snakes = new[]
        {
            Snake(SnakeColor.Red, (5, 5), (4, 5)),  // head at (5,5), right-facing
            Snake(SnakeColor.Cyan, (7, 5), (8, 5)), // head at (7,5)
            Snake(SnakeColor.Yellow, (2, 5), (1, 5)),
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), Direction.Right, excludedIndex: 0);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Prefers_the_closer_candidate_in_the_direction()
    {
        var snakes = new[]
        {
            Snake(SnakeColor.Red, (5, 5), (4, 5)),
            Snake(SnakeColor.Cyan, (9, 5), (10, 5)),
            Snake(SnakeColor.Yellow, (7, 5), (8, 5)),
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), Direction.Right, 0);
        Assert.Equal(2, result);
    }

    [Fact]
    public void Returns_null_when_no_snake_is_in_the_direction()
    {
        var snakes = new[]
        {
            Snake(SnakeColor.Red, (5, 5), (4, 5)),
            Snake(SnakeColor.Cyan, (3, 5), (4, 5)),   // to the left
            Snake(SnakeColor.Yellow, (1, 2), (0, 2)), // also to the left
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), Direction.Right, 0);
        Assert.Null(result);
    }

    [Fact]
    public void Excludes_the_currently_selected_snake()
    {
        var snakes = new[]
        {
            Snake(SnakeColor.Red, (5, 5), (4, 5)),
            Snake(SnakeColor.Cyan, (9, 5), (10, 5)),
        };
        // Excluding index 1 leaves the selection unchanged.
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), Direction.Right, excludedIndex: 1);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(Direction.Up, 5, 2, 0)]
    [InlineData(Direction.Down, 5, 8, 1)]
    [InlineData(Direction.Left, 2, 5, 2)]
    [InlineData(Direction.Right, 8, 5, 3)]
    public void Each_direction_selects_the_matching_neighbour(
        Direction direction, int expectedX, int expectedY, int expectedIndex)
    {
        var snakes = new[]
        {
            Snake(SnakeColor.Red, (5, 2), (4, 2)),   // above origin
            Snake(SnakeColor.Cyan, (5, 8), (4, 8)),  // below origin
            Snake(SnakeColor.Yellow, (2, 5), (1, 5)), // left of origin
            Snake(SnakeColor.Green, (8, 5), (7, 5)),  // right of origin
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), direction, excludedIndex: null);
        Assert.Equal(expectedIndex, result);
        Assert.Equal(new Cell(expectedX, expectedY), snakes[result!.Value].Head);
    }

    [Fact]
    public void Snake_outside_the_direction_cone_is_not_picked_when_a_cone_candidate_exists()
    {
        // A right-arrow from (5,5) must not jump to (6,10) when (8,5) is also
        // on the right — the former is mostly "down", not "right".
        var snakes = new[]
        {
            Snake(SnakeColor.Red, (5, 5), (4, 5)),
            Snake(SnakeColor.Cyan, (6, 10), (6, 11)),  // right-ish but mostly down
            Snake(SnakeColor.Yellow, (8, 5), (9, 5)), // cleanly right
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), Direction.Right, 0);
        Assert.Equal(2, result);
    }

    [Fact]
    public void Snakes_outside_the_cone_are_rejected_even_if_closer()
    {
        // Only candidate is (6, 10) which is mostly below — it's outside the
        // right-direction cone so we return null rather than snapping to it.
        var snakes = new[]
        {
            Snake(SnakeColor.Red, (5, 5), (4, 5)),
            Snake(SnakeColor.Cyan, (6, 10), (6, 11)),
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), Direction.Right, 0);
        Assert.Null(result);
    }
}
