using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TerminalSnake.Input;

// Direct P/Invoke wrapper around the Win32 console mode API. Without
// this, stdin on Windows stays in cooked mode (ENABLE_LINE_INPUT +
// ENABLE_ECHO_INPUT) so ReadFile blocks until Enter and the engine
// never sees Tab, Enter, Q, arrow keys, or mouse events. We also flip
// stdout into VT processing mode so the ANSI escape sequences Spectre
// and TerminalEscapeSequences emit (cursor hide, mouse enable, screen
// clear) reach the terminal as control sequences instead of literal
// characters.
//
// Only Windows is implemented here; non-Windows callers are no-ops.
[ExcludeFromCodeCoverage]
internal static partial class WindowsTerminal
{
    private const int StdInputHandle = -10;
    private const int StdOutputHandle = -11;

    private const uint EnableProcessedInput = 0x0001;
    private const uint EnableLineInput = 0x0002;
    private const uint EnableEchoInput = 0x0004;
    private const uint EnableMouseInput = 0x0010;
    private const uint EnableQuickEditMode = 0x0040;
    private const uint EnableExtendedFlags = 0x0080;
    private const uint EnableVirtualTerminalInput = 0x0200;

    private const uint EnableVirtualTerminalProcessing = 0x0004;
    private const uint DisableNewlineAutoReturn = 0x0008;

    private static readonly IntPtr InvalidHandle = new(-1);

    private static bool _hasSaved;
    private static uint _savedInputMode;
    private static uint _savedOutputMode;
    private static IntPtr _stdin;
    private static IntPtr _stdout;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    public static bool EnterRawMode()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        _stdin = GetStdHandle(StdInputHandle);
        _stdout = GetStdHandle(StdOutputHandle);
        if (_stdin == IntPtr.Zero || _stdin == InvalidHandle ||
            _stdout == IntPtr.Zero || _stdout == InvalidHandle)
        {
            return false;
        }

        if (!GetConsoleMode(_stdin, out _savedInputMode) ||
            !GetConsoleMode(_stdout, out _savedOutputMode))
        {
            return false;
        }
        _hasSaved = true;

        // Strip line discipline + echo so single keystrokes deliver
        // immediately, kill QuickEdit so left-clicks no longer get
        // intercepted as text-selection, and add VT input + mouse so
        // SGR-1006 mouse reports and ESC[A-style arrow sequences reach
        // the byte stream. ENABLE_PROCESSED_INPUT stays on so Ctrl+C
        // still routes through the .NET CancelKeyPress handler.
        var input = (_savedInputMode
                     & ~EnableLineInput
                     & ~EnableEchoInput
                     & ~EnableQuickEditMode)
                    | EnableExtendedFlags
                    | EnableMouseInput
                    | EnableVirtualTerminalInput
                    | EnableProcessedInput;

        var output = _savedOutputMode
                     | EnableVirtualTerminalProcessing
                     | DisableNewlineAutoReturn;

        return SetConsoleMode(_stdin, input) && SetConsoleMode(_stdout, output);
    }

    public static void Restore()
    {
        if (!OperatingSystem.IsWindows() || !_hasSaved)
        {
            return;
        }
        SetConsoleMode(_stdin, _savedInputMode);
        SetConsoleMode(_stdout, _savedOutputMode);
        _hasSaved = false;
    }
}
