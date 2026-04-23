using TerminalSnake.Game;
using TerminalSnake.Generation;

namespace TerminalSnake.Tests.Game;

public sealed class LevelManagerTests
{
    [Fact]
    public void Rejects_level_index_below_one()
    {
        var manager = new LevelManager();
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.LoadLevel(0));
    }

    [Fact]
    public void Levels_one_to_ten_come_from_fixed_levels()
    {
        var manager = new LevelManager();
        for (var i = 1; i <= FixedLevels.Count; i++)
        {
            var fromManager = manager.LoadLevel(i);
            var fromFixed = FixedLevels.Get(i);
            Assert.Equal(fromFixed.Size, fromManager.Size);
            Assert.Equal(fromFixed.Snakes.Length, fromManager.Snakes.Length);
        }
    }

    [Fact]
    public void Levels_above_fixed_range_use_generator_and_stay_solvable()
    {
        var manager = new LevelManager();
        for (var i = FixedLevels.Count + 1; i <= FixedLevels.Count + 3; i++)
        {
            var board = manager.LoadLevel(i);
            Assert.NotNull(Solver.TrySolve(board));
        }
    }

    [Fact]
    public void Custom_generator_is_used_for_levels_above_fixed_range()
    {
        var manager = new LevelManager(new BoardGenerator());
        var board = manager.LoadLevel(FixedLevels.Count + 1);
        Assert.NotNull(board);
    }

    [Fact]
    public void Null_generator_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new LevelManager(null!));
    }
}
