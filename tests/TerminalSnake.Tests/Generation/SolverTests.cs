using TerminalSnake.Domain;
using TerminalSnake.Generation;
using TerminalSnake.Movement;

namespace TerminalSnake.Tests.Generation;

public sealed class SolverTests
{
    private static Snake Snake(SnakeColor color, params (int X, int Y)[] cells) =>
        new(cells.Select(c => new Cell(c.X, c.Y)), color);

    [Fact]
    public void Empty_board_is_trivially_solved()
    {
        var board = new Board(5, Array.Empty<Snake>());
        var solution = Solver.TrySolve(board);
        Assert.NotNull(solution);
        Assert.Empty(solution);
    }

    [Fact]
    public void Single_snake_with_clear_exit_solves_in_one_step()
    {
        var snake = Snake(SnakeColor.Red, (2, 2), (1, 2));
        var board = new Board(5, new[] { snake });

        var solution = Solver.TrySolve(board);

        Assert.NotNull(solution);
        Assert.Single(solution);
        Assert.Equal(0, solution[0]);
    }

    [Fact]
    public void Two_snakes_in_line_require_the_leader_to_exit_first()
    {
        var trailing = Snake(SnakeColor.Red, (1, 2), (0, 2));
        var leader = Snake(SnakeColor.Cyan, (3, 2), (2, 2));
        var board = new Board(5, new[] { trailing, leader });

        var solution = Solver.TrySolve(board);

        Assert.NotNull(solution);
        var replayed = Replay(board, solution);
        Assert.Empty(replayed.Snakes);
    }

    [Fact]
    public void Three_parallel_snakes_all_exit()
    {
        var snakes = new[]
        {
            Snake(SnakeColor.Red,    (2, 0), (1, 0)),
            Snake(SnakeColor.Cyan,   (2, 2), (1, 2)),
            Snake(SnakeColor.Yellow, (2, 4), (1, 4)),
        };
        var board = new Board(5, snakes);

        var solution = Solver.TrySolve(board);

        Assert.NotNull(solution);
        Assert.Equal(3, solution.Count);
        Assert.Empty(Replay(board, solution).Snakes);
    }

    [Fact]
    public void Deadlock_between_two_opposing_snakes_is_unsolvable()
    {
        // Red heads right from (1,2); Cyan heads left from (3,2). They will meet head-to-head.
        var red = Snake(SnakeColor.Red, (1, 2), (0, 2));
        var cyan = Snake(SnakeColor.Cyan, (3, 2), (4, 2));
        var board = new Board(5, new[] { red, cyan });

        var solution = Solver.TrySolve(board);

        Assert.Null(solution);
    }

    [Fact]
    public void Head_on_collision_with_three_snakes_is_unsolvable()
    {
        // Red right-from-(1,2); Cyan left-from-(3,2); Yellow right-from-(1,0).
        // Yellow exits freely, but Red+Cyan remain deadlocked head-on.
        var red = Snake(SnakeColor.Red, (1, 2), (0, 2));
        var cyan = Snake(SnakeColor.Cyan, (3, 2), (4, 2));
        var yellow = Snake(SnakeColor.Yellow, (1, 0), (0, 0));
        var board = new Board(5, new[] { red, cyan, yellow });

        Assert.Null(Solver.TrySolve(board));
    }

    [Fact]
    public void Solver_returns_null_when_state_limit_is_tiny()
    {
        var snakes = new[]
        {
            Snake(SnakeColor.Red,    (2, 0), (1, 0)),
            Snake(SnakeColor.Cyan,   (2, 2), (1, 2)),
            Snake(SnakeColor.Yellow, (2, 4), (1, 4)),
            Snake(SnakeColor.Green,  (4, 1), (4, 0)),
        };
        var board = new Board(6, snakes);

        var solution = Solver.TrySolve(board, stateLimit: 1);
        Assert.Null(solution);
    }

    [Fact]
    public void Solver_throws_for_null_board()
    {
        Assert.Throws<ArgumentNullException>(() => Solver.TrySolve(null!));
    }

    private static Board Replay(Board start, IReadOnlyList<int> sequence)
    {
        var board = start;
        foreach (var index in sequence)
        {
            var outcome = MoveEngine.Advance(board, index);
            Assert.True(outcome.Steps > 0, $"replay step at index {index} made no progress");
            board = outcome.ResultingBoard;
        }
        return board;
    }
}
