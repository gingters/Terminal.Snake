using TerminalSnake.Generation;

namespace TerminalSnake.Tests.Generation;

public sealed class FixedLevelsTests
{
    [Fact]
    public void Count_is_ten()
    {
        Assert.Equal(10, FixedLevels.Count);
        Assert.Equal(10, FixedLevels.All().Count);
    }

    [Fact]
    public void Get_rejects_out_of_range_indices()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedLevels.Get(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedLevels.Get(11));
    }

    [Fact]
    public void Every_fixed_level_is_solvable()
    {
        var levels = FixedLevels.All();
        for (var i = 0; i < levels.Count; i++)
        {
            var solution = Solver.TrySolve(levels[i]);
            Assert.True(solution is not null, $"Level {i + 1} (index {i}) is not solvable");
        }
    }
}
