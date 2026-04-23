using TerminalSnake.Game;

namespace TerminalSnake.Tests.Game;

public sealed class IdleWatcherTests
{
    [Fact]
    public void Constructor_rejects_non_positive_threshold()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdleWatcher(TimeSpan.Zero));
    }

    [Fact]
    public void Default_state_treats_zero_time_as_idle()
    {
        var watcher = new IdleWatcher(TimeSpan.FromSeconds(30));
        Assert.True(watcher.HasIdledOut(TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void Note_activity_resets_the_idle_window()
    {
        var watcher = new IdleWatcher(TimeSpan.FromSeconds(30));
        watcher.NoteActivity(TimeSpan.FromSeconds(5));
        Assert.False(watcher.HasIdledOut(TimeSpan.FromSeconds(20)));
        Assert.True(watcher.HasIdledOut(TimeSpan.FromSeconds(35)));
    }

    [Fact]
    public void Threshold_is_exposed()
    {
        var watcher = new IdleWatcher(TimeSpan.FromSeconds(42));
        Assert.Equal(TimeSpan.FromSeconds(42), watcher.Threshold);
    }
}
