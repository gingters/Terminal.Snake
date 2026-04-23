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
}
