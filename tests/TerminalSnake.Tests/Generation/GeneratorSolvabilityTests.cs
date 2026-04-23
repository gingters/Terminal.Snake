using TerminalSnake.Generation;

namespace TerminalSnake.Tests.Generation;

public sealed class GeneratorSolvabilityTests
{
    public static IEnumerable<object[]> LevelCases =>
        Enumerable.Range(1, 20).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(LevelCases))]
    public void Twenty_seeds_per_level_are_all_solvable(int levelIndex)
    {
        var generator = new BoardGenerator();
        for (var seed = 0; seed < 20; seed++)
        {
            var board = generator.Generate(levelIndex, seed);
            Assert.True(
                Solver.TrySolve(board) is not null,
                $"Level {levelIndex} seed {seed} is not solvable");
        }
    }

    [Fact]
    public void Stress_over_five_hundred_random_seeds_across_levels()
    {
        var generator = new BoardGenerator();
        var random = new Random(2026);
        for (var i = 0; i < 500; i++)
        {
            var levelIndex = random.Next(1, 16);
            var seed = random.Next();
            var board = generator.Generate(levelIndex, seed);
            Assert.True(
                Solver.TrySolve(board) is not null,
                $"Stress: level {levelIndex} seed {seed} is not solvable");
        }
    }
}
