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
    // Clear the visible screen and move the cursor home. Emitted on
    // shutdown so the board is not left on the terminal after quit (#22).
    public const string ClearScreen = "\x1b[2J\x1b[H";
}

// Flips the host tty out of canonical+echo mode so keystrokes reach the app
// immediately, enables xterm mouse reporting, hides the cursor, and
// restores the previous state on Dispose. Excluded from coverage because
// all its behaviour depends on a live terminal.
[ExcludeFromCodeCoverage]
public sealed class TerminalMode : IDisposable
{
    private bool _enabled;

    public void Enable(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        PosixTerminal.EnterRawMode();
        WindowsTerminal.EnterRawMode();
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
        output.Write(TerminalEscapeSequences.ClearScreen);
        output.Write(TerminalEscapeSequences.ShowCursor);
        output.Write(TerminalEscapeSequences.DisableMouse);
        output.Flush();
        WindowsTerminal.Restore();
        PosixTerminal.Restore();
        _enabled = false;
    }

    public void Dispose() => Disable(Console.Out);
}
