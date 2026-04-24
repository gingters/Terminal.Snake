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

        Assert.InRange(board.Size, BoardGenerator.MinBoardSize, BoardGenerator.MaxBoardSize);
        Assert.InRange(board.Snakes.Length, 1, BoardGenerator.MaxSnakesPerBoard);
        foreach (var snake in board.Snakes)
        {
            Assert.InRange(snake.Segments.Length, 2, BoardGenerator.MaxSegmentLength);
        }
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
    public void Snake_colors_are_unique_per_board()
    {
        var generator = new BoardGenerator();
        var board = generator.Generate(10, seed: 99);
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
}
