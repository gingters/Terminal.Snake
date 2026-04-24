using System.Diagnostics;
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

// Flips the host tty out of canonical+echo mode so keystrokes reach the app
// immediately, enables xterm mouse reporting, hides the cursor, and restores
// the previous state on Dispose. Excluded from coverage because all its
// behaviour depends on a live terminal; the argument-formatting is covered
// indirectly by TerminalEscapeSequencesTests.
[ExcludeFromCodeCoverage]
public sealed class TerminalMode : IDisposable
{
    private bool _enabled;
    private string? _savedPosixTty;

    public void Enable(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        SwitchStdinToRaw();
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
        RestoreStdin();
        _enabled = false;
    }

    public void Dispose() => Disable(Console.Out);

    private void SwitchStdinToRaw()
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows the default console is already line-buffered, but
            // Console.ReadKey / the equivalent Win32 SetConsoleMode path is
            // not exercised here. Left intentionally empty; tracked
            // separately.
            return;
        }
        _savedPosixTty = CaptureStty();
        RunStty("-icanon -echo -isig min 1 time 0");
    }

    private void RestoreStdin()
    {
        if (OperatingSystem.IsWindows() || string.IsNullOrEmpty(_savedPosixTty))
        {
            return;
        }
        RunStty(_savedPosixTty);
        _savedPosixTty = null;
    }

    private static string? CaptureStty()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("/bin/stty", "-g")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (proc is null)
            {
                return null;
            }
            var state = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(1000);
            return string.IsNullOrWhiteSpace(state) ? null : state;
        }
        catch
        {
            return null;
        }
    }

    private static void RunStty(string arguments)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo("/bin/stty", arguments)
            {
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            proc?.WaitForExit(1000);
        }
        catch
        {
            // Best effort — if stty is unavailable the game still runs, just in line mode.
        }
    }
}
