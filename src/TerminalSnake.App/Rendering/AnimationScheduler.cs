using System.Collections.Immutable;
using TerminalSnake.Domain;

namespace TerminalSnake.Rendering;

public sealed class AnimationScheduler
{
    private readonly TimeSpan _stepDuration;
    private ActiveAnimation? _active;

    public AnimationScheduler(TimeSpan stepDuration)
    {
        if (stepDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepDuration), stepDuration, "Step duration must be positive");
        }
        _stepDuration = stepDuration;
    }

    public bool IsBusy => _active is not null;

    public void Start(int snakeIndex, ImmutableArray<ImmutableArray<Cell>> frames, TimeSpan now)
    {
        if (frames.IsDefaultOrEmpty)
        {
            return;
        }
        _active = new ActiveAnimation(snakeIndex, frames, now);
    }

    public AnimationSnapshot? Current(TimeSpan now)
    {
        if (_active is null)
        {
            return null;
        }
        var index = FrameIndex(_active.Value, now);
        if (index >= _active.Value.Frames.Length)
        {
            return null;
        }
        return new AnimationSnapshot(_active.Value.SnakeIndex, _active.Value.Frames[index]);
    }

    public bool IsComplete(TimeSpan now)
    {
        if (_active is null)
        {
            return true;
        }
        return FrameIndex(_active.Value, now) >= _active.Value.Frames.Length;
    }

    public void Clear() => _active = null;

    private int FrameIndex(ActiveAnimation animation, TimeSpan now)
    {
        var elapsed = now - animation.StartTime;
        if (elapsed.Ticks < 0)
        {
            return 0;
        }
        return (int)(elapsed.Ticks / _stepDuration.Ticks);
    }

    private readonly record struct ActiveAnimation(
        int SnakeIndex,
        ImmutableArray<ImmutableArray<Cell>> Frames,
        TimeSpan StartTime);
}

public readonly record struct AnimationSnapshot(int SnakeIndex, ImmutableArray<Cell> Segments);
