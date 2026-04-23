using System.Diagnostics.CodeAnalysis;

namespace TerminalSnake.Input;

// Builds the ANSI escape sequences needed to toggle xterm mouse reporting
// on and off. Kept pure so it can be unit-tested without a real terminal.
public static class TerminalEscapeSequences
{
    public const string EnableMouse = "\x1b[?1000h\x1b[?1006h";
    public const string DisableMouse = "\x1b[?1006l\x1b[?1000l";
    public const string HideCursor = "\x1b[?25l";
    public const string ShowCursor = "\x1b[?25h";
}

// Thin wrapper that writes the TerminalEscapeSequences strings to the console
// and flips stdin into raw mode on POSIX. Excluded from coverage because it
// only reaches code via real terminal state changes that cannot be unit-tested.
[ExcludeFromCodeCoverage]
public sealed class TerminalMode : IDisposable
{
    private bool _enabled;

    public void Enable(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        output.Write(TerminalEscapeSequences.EnableMouse);
        output.Write(TerminalEscapeSequences.HideCursor);
        output.Flush();
        _enabled = true;
    }

    public void Disable(TextWriter output)
    {
        if (!_enabled)
        {
            return;
        }
        output.Write(TerminalEscapeSequences.ShowCursor);
        output.Write(TerminalEscapeSequences.DisableMouse);
        output.Flush();
        _enabled = false;
    }

    public void Dispose() => Disable(Console.Out);
}
