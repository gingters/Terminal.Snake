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
    public void Narrow_cone_ignores_a_sideways_closer_snake_in_favour_of_a_straight_one()
    {
        // Reproduces the bug reported after the first #19 iteration: pressing
        // Down on the yellow snake (origin) must select the blue snake that
        // lies straight below rather than the red snake that is only a row
        // closer but two columns to the side. The narrow-funnel filter
        // rejects the sideways candidate at short projection.
        var snakes = new[]
        {
            Snake(SnakeColor.Yellow, (8, 2), (9, 2)),  // origin
            Snake(SnakeColor.Red,    (6, 4), (7, 4)),  // 2 down, 2 left
            Snake(SnakeColor.Blue,   (8, 7), (9, 7)),  // 5 down, 0 side
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(8, 2), Direction.Down, excludedIndex: 0);
        Assert.Equal(2, result);
    }

    [Fact]
    public void Narrow_cone_widens_at_distance_so_far_snakes_are_still_reachable()
    {
        // At projection 6 the funnel is wide enough to accept a candidate 3
        // columns off-axis.
        var snakes = new[]
        {
            Snake(SnakeColor.Yellow, (5, 5), (4, 5)),
            Snake(SnakeColor.Blue,   (8, 11), (9, 11)), // 6 down, 3 right
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), Direction.Down, excludedIndex: 0);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Half_plane_fallback_picks_the_lone_off_axis_candidate()
    {
        // Only candidate is (6, 10) — off-axis for a right-press but still
        // on the "right" side (dx > 0). The narrow cone rejects it, but the
        // half-plane fallback kicks in so the player is not stuck without
        // a selection target.
        var snakes = new[]
        {
            Snake(SnakeColor.Red, (5, 5), (4, 5)),
            Snake(SnakeColor.Cyan, (6, 10), (6, 11)),
        };
        var result = SnakeSelection.FindNearestInDirection(snakes, new Cell(5, 5), Direction.Right, 0);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Corner_snake_still_reaches_off_axis_neighbours_via_fallback()
    {
        // Reproduces the second image-based report on issue #19: the red
        // snake sits in a corner and the other two snakes are both far
        // off-axis. Without the half-plane fallback neither up nor down
        // would find anything, which makes arrow navigation feel broken.
        var snakes = new[]
        {
            Snake(SnakeColor.Red,    (2, 8), (1, 8)),
            Snake(SnakeColor.Yellow, (9, 2), (8, 2)),   // up-right of red
            Snake(SnakeColor.Blue,   (10, 9), (11, 9)), // down-right of red
        };
        var up = SnakeSelection.FindNearestInDirection(snakes, new Cell(2, 8), Direction.Up, excludedIndex: 0);
        Assert.Equal(1, up);
        var down = SnakeSelection.FindNearestInDirection(snakes, new Cell(2, 8), Direction.Down, excludedIndex: 0);
        Assert.Equal(2, down);
    }
}
