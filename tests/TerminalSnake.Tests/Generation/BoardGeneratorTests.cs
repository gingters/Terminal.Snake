using TerminalSnake.Domain;
using TerminalSnake.Generation;

namespace TerminalSnake.Tests.Generation;

public sealed class BoardGeneratorTests
{
    [Fact]
    public void Rejects_level_index_below_one()
    {
        var generator = new BoardGenerator();
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(0, 0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    public void Generated_board_has_valid_snakes_and_size(int levelIndex)
    {
        var generator = new BoardGenerator();
        var board = generator.Generate(levelIndex, seed: 42);

        // Default (no explicit terminal cap) keeps the board within the
        // original [MinBoardSize, MaxBoardSize] range so tutorial-tight
        // density is preserved for tests and fallback paths.
        Assert.InRange(board.Size, BoardGenerator.MinBoardSize, BoardGenerator.MaxBoardSize);
        Assert.InRange(board.Snakes.Length, 1, BoardGenerator.MaxSnakesPerBoard);
        foreach (var snake in board.Snakes)
        {
            Assert.InRange(snake.Segments.Length, 2, BoardGenerator.MaxSegmentLength);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Snakes_sit_at_least_one_cell_away_from_every_border(int levelIndex)
    {
        // #36: the outermost snake cell must leave at least one blank cell
        // before the border so the play area no longer feels packed.
        var generator = new BoardGenerator();
        var board = generator.Generate(levelIndex, seed: 42);
        foreach (var snake in board.Snakes)
        {
            foreach (var cell in snake.Segments)
            {
                Assert.InRange(cell.X, 1, board.Size - 2);
                Assert.InRange(cell.Y, 1, board.Size - 2);
            }
        }
    }

    [Fact]
    public void Board_grows_with_the_level_when_the_terminal_allows_it()
    {
        // #36 follow-up: on big terminals the board must keep growing with
        // the level, not plateau at the default placement cap. With a
        // large maxBoardSide, level 50 produces a much bigger board than
        // level 1.
        var generator = new BoardGenerator();
        var small = generator.Generate(1, seed: 42, maxBoardSide: 40);
        var large = generator.Generate(50, seed: 42, maxBoardSide: 40);
        Assert.True(large.Size > small.Size, $"level 50 ({large.Size}) should be larger than level 1 ({small.Size})");
        Assert.True(large.Size <= 40, "board never exceeds the supplied terminal cap");
    }

    [Fact]
    public void Snake_count_and_length_scale_up_on_big_terminals()
    {
        // #36 follow-up — #36 image review: level 999 on a 40x40 board
        // used to ship 8 stubby snakes clustered in the middle. The
        // generator now scales both the snake count and the max length
        // with the actual board size so the play area actually gets used.
        var generator = new BoardGenerator();
        var huge = generator.Generate(999, seed: 42, maxBoardSide: 40);
        var longest = huge.Snakes.Max(s => s.Segments.Length);
        Assert.True(huge.Snakes.Length >= 9,
            $"expected ≥ 9 snakes on the huge board, got {huge.Snakes.Length}");
        Assert.True(longest >= 20,
            $"expected the longest snake to be ≥ 20 cells, got {longest}");
    }

    [Fact]
    public void Same_seed_and_level_produces_deterministic_board()
    {
        var generator = new BoardGenerator();
        var first = generator.Generate(5, seed: 1234);
        var second = generator.Generate(5, seed: 1234);

        Assert.Equal(first.Size, second.Size);
        Assert.Equal(first.Snakes.Length, second.Snakes.Length);
        for (var i = 0; i < first.Snakes.Length; i++)
        {
            Assert.Equal(first.Snakes[i].Color, second.Snakes[i].Color);
            Assert.Equal(first.Snakes[i].Segments, second.Snakes[i].Segments);
        }
    }

    [Fact]
    public void Snake_colors_are_unique_on_tutorial_sized_boards()
    {
        // Below 8 snakes every colour is distinct — past that #36 / #38
        // allow colour cycling since the SnakeColor palette is only
        // eight entries and the denser mid/late boards need more
        // snakes than that.
        var generator = new BoardGenerator();
        var board = generator.Generate(2, seed: 99);
        Assert.True(board.Snakes.Length <= 8);
        var colors = board.Snakes.Select(s => s.Color).ToArray();
        Assert.Equal(colors.Length, colors.Distinct().Count());
    }

    [Fact]
    public void Rejects_negative_solvable_attempts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoardGenerator(-1));
    }

    [Fact]
    public void Falls_back_to_deterministic_board_when_solvable_attempts_is_zero()
    {
        var generator = new BoardGenerator(solvableAttempts: 0);
        var board = generator.Generate(3, seed: 1);
        Assert.True(board.Snakes.Length >= 1);
        foreach (var snake in board.Snakes)
        {
            Assert.Equal(2, snake.Segments.Length);
            Assert.Equal(Direction.Right, snake.Direction);
        }
    }

    [Fact]
    public void Mid_level_boards_include_snakes_of_at_least_five_segments()
    {
        // Issue #13: from ~level 5 onwards at least one snake on most boards
        // must be 5+ cells. Scan 10 seeds and require the claim to hold on
        // at least one of them (generator randomness leaves room for the
        // occasional all-short board).
        var generator = new BoardGenerator();
        var found = Enumerable.Range(0, 10)
            .Select(seed => generator.Generate(6, seed))
            .Any(b => b.Snakes.Any(s => s.Segments.Length >= 5));
        Assert.True(found, "level 6 should be able to produce a 5+ cell snake");
    }

    [Fact]
    public void High_level_boards_include_snakes_of_at_least_ten_segments()
    {
        // Issue #13: later levels should produce genuinely long snakes.
        // Scan 10 seeds and require at least one to yield a 10+ cell snake.
        var generator = new BoardGenerator();
        var found = Enumerable.Range(0, 10)
            .Select(seed => generator.Generate(12, seed))
            .Any(b => b.Snakes.Any(s => s.Segments.Length >= 10));
        Assert.True(found, "level 12 should be able to produce a 10+ cell snake");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public void Level_two_and_above_always_start_with_at_least_one_blocked_snake(int levelIndex)
    {
        // Issue #14: boards where every snake can walk straight out trivialise
        // the puzzle. The generator should reject them from level 2 onwards.
        var generator = new BoardGenerator();
        for (var seed = 0; seed < 10; seed++)
        {
            var board = generator.Generate(levelIndex, seed);
            Assert.True(
                HasAnyInitiallyBlockedSnake(board),
                $"level {levelIndex} seed {seed} has no initially blocked snake");
        }
    }

    private static bool HasAnyInitiallyBlockedSnake(Board board)
    {
        foreach (var snake in board.Snakes)
        {
            var delta = snake.Direction.Delta();
            var cursor = snake.Head + delta;
            while (board.IsInside(cursor))
            {
                var occupant = board.OccupyingSnake(cursor);
                if (occupant is int index && !ReferenceEquals(board.Snakes[index], snake))
                {
                    return true;
                }
                cursor += delta;
            }
        }
        return false;
    }
}
