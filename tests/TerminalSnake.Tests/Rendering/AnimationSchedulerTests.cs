using System.Collections.Immutable;
using TerminalSnake.Domain;
using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Rendering;

public sealed class AnimationSchedulerTests
{
    private static ImmutableArray<ImmutableArray<Cell>> MakeFrames(params (int X, int Y)[] heads)
    {
        return heads.Select(h => ImmutableArray.Create(new Cell(h.X, h.Y), new Cell(h.X - 1, h.Y)))
            .ToImmutableArray();
    }

    [Fact]
    public void Constructor_rejects_non_positive_step_duration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnimationScheduler(TimeSpan.Zero));
    }

    [Fact]
    public void IsBusy_defaults_to_false()
    {
        var scheduler = new AnimationScheduler(TimeSpan.FromMilliseconds(100));
        Assert.False(scheduler.IsBusy);
    }

    [Fact]
    public void IsComplete_defaults_to_true_when_no_animation_active()
    {
        var scheduler = new AnimationScheduler(TimeSpan.FromMilliseconds(100));
        Assert.True(scheduler.IsComplete(TimeSpan.Zero));
    }

    [Fact]
    public void Start_records_animation_and_reports_first_frame_immediately()
    {
        var scheduler = new AnimationScheduler(TimeSpan.FromMilliseconds(100));
        var frames = MakeFrames((2, 2), (3, 2), (4, 2));

        scheduler.Start(snakeIndex: 0, frames, TimeSpan.Zero);
        Assert.True(scheduler.IsBusy);
        var snapshot = scheduler.Current(TimeSpan.Zero);

        Assert.NotNull(snapshot);
        Assert.Equal(0, snapshot.Value.SnakeIndex);
        Assert.Equal(new Cell(2, 2), snapshot.Value.Segments[0]);
    }

    [Fact]
    public void Current_advances_through_frames_as_time_passes()
    {
        var scheduler = new AnimationScheduler(TimeSpan.FromMilliseconds(100));
        var frames = MakeFrames((2, 2), (3, 2), (4, 2));
        scheduler.Start(0, frames, TimeSpan.Zero);

        Assert.Equal(new Cell(3, 2), scheduler.Current(TimeSpan.FromMilliseconds(150))!.Value.Segments[0]);
        Assert.Equal(new Cell(4, 2), scheduler.Current(TimeSpan.FromMilliseconds(250))!.Value.Segments[0]);
    }

    [Fact]
    public void Current_returns_null_once_frames_are_exhausted()
    {
        var scheduler = new AnimationScheduler(TimeSpan.FromMilliseconds(100));
        var frames = MakeFrames((2, 2), (3, 2));
        scheduler.Start(0, frames, TimeSpan.Zero);

        Assert.NotNull(scheduler.Current(TimeSpan.FromMilliseconds(150)));
        Assert.Null(scheduler.Current(TimeSpan.FromMilliseconds(201)));
        Assert.True(scheduler.IsComplete(TimeSpan.FromMilliseconds(201)));
    }

    [Fact]
    public void Start_with_empty_frames_does_not_mark_busy()
    {
        var scheduler = new AnimationScheduler(TimeSpan.FromMilliseconds(100));
        scheduler.Start(0, ImmutableArray<ImmutableArray<Cell>>.Empty, TimeSpan.Zero);
        Assert.False(scheduler.IsBusy);
    }

    [Fact]
    public void Clear_cancels_running_animation()
    {
        var scheduler = new AnimationScheduler(TimeSpan.FromMilliseconds(100));
        var frames = MakeFrames((2, 2), (3, 2));
        scheduler.Start(0, frames, TimeSpan.Zero);
        scheduler.Clear();
        Assert.False(scheduler.IsBusy);
        Assert.Null(scheduler.Current(TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public void Current_before_start_time_returns_first_frame()
    {
        var scheduler = new AnimationScheduler(TimeSpan.FromMilliseconds(100));
        var frames = MakeFrames((2, 2), (3, 2));
        scheduler.Start(0, frames, TimeSpan.FromMilliseconds(500));
        var snapshot = scheduler.Current(TimeSpan.FromMilliseconds(400));
        Assert.Equal(new Cell(2, 2), snapshot!.Value.Segments[0]);
    }
}
