namespace TerminalSnake.Game;

public sealed class IdleWatcher
{
    private readonly TimeSpan _threshold;
    private TimeSpan _lastActivity;

    public IdleWatcher(TimeSpan threshold)
    {
        if (threshold <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold), threshold, "Idle threshold must be positive");
        }
        _threshold = threshold;
    }

    public TimeSpan Threshold => _threshold;

    public void NoteActivity(TimeSpan now)
    {
        _lastActivity = now;
    }

    public bool HasIdledOut(TimeSpan now) => now - _lastActivity >= _threshold;
}
