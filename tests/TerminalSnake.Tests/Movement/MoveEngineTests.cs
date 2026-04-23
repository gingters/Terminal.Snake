using TerminalSnake.Domain;
using TerminalSnake.Movement;

namespace TerminalSnake.Tests.Movement;

public sealed class MoveEngineTests
{
    private static Snake Snake(SnakeColor color, params (int X, int Y)[] cells) =>
        new(cells.Select(c => new Cell(c.X, c.Y)), color);

    [Fact]
    public void Rejects_null_board()
    {
        Assert.Throws<ArgumentNullException>(() => MoveEngine.Advance(null!, 0));
    }

    [Fact]
    public void Rejects_out_of_range_snake_index()
    {
        var board = new Board(5, new[] { Snake(SnakeColor.Red, (0, 0), (1, 0)) });
        Assert.Throws<ArgumentOutOfRangeException>(() => MoveEngine.Advance(board, 1));
    }

    [Fact]
    public void Snake_moving_right_exits_cleanly_on_empty_row()
    {
        var snake = Snake(SnakeColor.Red, (1, 2), (0, 2));
        var board = new Board(5, new[] { snake });

        var outcome = MoveEngine.Advance(board, 0);

        Assert.True(outcome.Exited);
        Assert.Null(outcome.BlockedBy);
        Assert.Empty(outcome.ResultingBoard.Snakes);
        // 3 inside moves (1->2, 2->3, 3->4) then 2 outside steps until the last segment is gone.
        Assert.Equal(5, outcome.Steps);
    }

    [Theory]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    public void Snake_exits_in_each_of_the_four_directions(Direction direction)
    {
        var head = new Cell(2, 2);
        var tail = head + direction.Opposite().Delta();
        var snake = new Snake(new[] { head, tail }, SnakeColor.Cyan);
        var board = new Board(5, new[] { snake });

        var outcome = MoveEngine.Advance(board, 0);

        Assert.True(outcome.Exited);
        Assert.Empty(outcome.ResultingBoard.Snakes);
    }

    [Fact]
    public void Snake_stops_when_blocked_by_another_snake()
    {
        var mover = Snake(SnakeColor.Red, (1, 0), (0, 0));
        var blocker = Snake(SnakeColor.Cyan, (3, 0), (3, 1));
        var board = new Board(5, new[] { mover, blocker });

        var outcome = MoveEngine.Advance(board, 0);

        Assert.False(outcome.Exited);
        Assert.Equal(1, outcome.BlockedBy);
        Assert.Equal(1, outcome.Steps);
        var survivor = outcome.ResultingBoard.Snakes[0];
        Assert.Equal(new Cell(2, 0), survivor.Head);
    }

    [Fact]
    public void Frames_record_each_successful_step()
    {
        var snake = Snake(SnakeColor.Red, (1, 2), (0, 2));
        var board = new Board(5, new[] { snake });

        var outcome = MoveEngine.Advance(board, 0);

        Assert.Equal(outcome.Steps, outcome.Frames.Length);
        Assert.Equal(new Cell(2, 2), outcome.Frames[0][0]);
    }

    [Fact]
    public void Frozen_when_directly_blocked()
    {
        var mover = Snake(SnakeColor.Red, (1, 0), (0, 0));
        var wall = Snake(SnakeColor.Cyan, (2, 0), (2, 1));
        var board = new Board(5, new[] { mover, wall });

        var outcome = MoveEngine.Advance(board, 0);

        Assert.Equal(0, outcome.Steps);
        Assert.Equal(1, outcome.BlockedBy);
        Assert.False(outcome.Moved);
        Assert.False(outcome.Exited);
    }

    [Fact]
    public void Second_snake_passes_through_once_leader_has_left()
    {
        // Two snakes on the same row heading right; the rightmost must exit first
        // so the one behind it can pass through freed cells.
        var trailing = Snake(SnakeColor.Red, (1, 2), (0, 2));
        var leader = Snake(SnakeColor.Cyan, (3, 2), (2, 2));
        var board = new Board(5, new[] { trailing, leader });

        var leaderOut = MoveEngine.Advance(board, 1);
        Assert.True(leaderOut.Exited);
        Assert.Single(leaderOut.ResultingBoard.Snakes);

        var trailingOut = MoveEngine.Advance(leaderOut.ResultingBoard, 0);
        Assert.True(trailingOut.Exited);
        Assert.Empty(trailingOut.ResultingBoard.Snakes);
    }

    [Fact]
    public void Blocked_snake_resumes_after_blocker_exits()
    {
        // Waiter would push into the blocker's tail cell (2,2) on its first step.
        var blocker = Snake(SnakeColor.Cyan, (3, 2), (2, 2));
        var waiter = Snake(SnakeColor.Red, (1, 2), (0, 2));
        var board = new Board(5, new[] { blocker, waiter });

        var waiterBlocked = MoveEngine.Advance(board, 1);
        Assert.Equal(0, waiterBlocked.Steps);
        Assert.Equal(0, waiterBlocked.BlockedBy);

        var blockerOut = MoveEngine.Advance(waiterBlocked.ResultingBoard, 0);
        Assert.True(blockerOut.Exited);

        var waiterOut = MoveEngine.Advance(blockerOut.ResultingBoard, 0);
        Assert.True(waiterOut.Exited);
    }

    [Fact]
    public void Self_collision_blocks_movement()
    {
        // Spiral snake whose forward ray runs through its own body.
        // Head (2,2) direction Right; body wraps so that (3,2) is the fifth segment.
        var snake = Snake(SnakeColor.Green,
            (2, 2), // head
            (1, 2), // segment[1] -> direction = Right
            (1, 3),
            (2, 3),
            (3, 3),
            (3, 2), // directly ahead of head
            (3, 1));
        var board = new Board(6, new[] { snake });

        var outcome = MoveEngine.Advance(board, 0);

        Assert.Equal(0, outcome.Steps);
        Assert.Equal(MoveEngineInternals.SelfBlockerValue, outcome.BlockedBy);
        Assert.False(outcome.Exited);
    }
}

internal static class MoveEngineInternals
{
    public const int SelfBlockerValue = -1;
}
