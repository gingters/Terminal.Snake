namespace TerminalSnake.Rendering;

public sealed class TerminalTooSmallException : Exception
{
    public TerminalTooSmallException(int requiredWidth, int requiredHeight, int actualWidth, int actualHeight)
        : base($"Terminal must be at least {requiredWidth}x{requiredHeight} (current: {actualWidth}x{actualHeight})")
    {
        RequiredWidth = requiredWidth;
        RequiredHeight = requiredHeight;
        ActualWidth = actualWidth;
        ActualHeight = actualHeight;
    }

    public int RequiredWidth { get; }

    public int RequiredHeight { get; }

    public int ActualWidth { get; }

    public int ActualHeight { get; }
}
