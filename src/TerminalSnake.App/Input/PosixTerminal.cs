using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TerminalSnake.Input;

// Direct P/Invoke wrapper around POSIX termios. Used to put stdin into
// character-at-a-time mode with echo disabled so the game loop can react
// to single keystrokes like Tab without waiting for the user to press
// Enter (which was the actual behaviour that survived #10's stty-based
// attempt on macOS).
//
// Only macOS (Darwin) is implemented; other platforms leave the tty
// alone and the app falls back to line-buffered input.
[ExcludeFromCodeCoverage]
internal static partial class PosixTerminal
{
    private const int StdinFileno = 0;
    private const int TcsaNow = 0;

    // Darwin termios constants (from <sys/termios.h>).
    private const ulong DarwinIcanon = 0x00000100;
    private const ulong DarwinEcho   = 0x00000008;
    private const ulong DarwinEchonl = 0x00000010;
    private const ulong DarwinIexten = 0x00000400;
    private const int DarwinVmin = 16;
    private const int DarwinVtime = 17;

    // macOS exposes the C library as libSystem.dylib; using the explicit
    // path keeps the AoT-generated P/Invoke stub from relying on the
    // runtime's libc → libSystem fallback, which is JIT-only and silently
    // fails to resolve under Native AoT (root cause of the released binary
    // staying in canonical mode regardless of EnterRawMode).
    private const string LibSystem = "/usr/lib/libSystem.dylib";

    private static bool _hasSaved;
    private static TermiosDarwin _saved;

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct TermiosDarwin
    {
        public ulong c_iflag;
        public ulong c_oflag;
        public ulong c_cflag;
        public ulong c_lflag;
        public fixed byte c_cc[20];
        public ulong c_ispeed;
        public ulong c_ospeed;
    }

    [LibraryImport(LibSystem, EntryPoint = "tcgetattr", SetLastError = true)]
    private static unsafe partial int TcGetAttr(int fd, TermiosDarwin* termios);

    [LibraryImport(LibSystem, EntryPoint = "tcsetattr", SetLastError = true)]
    private static unsafe partial int TcSetAttr(int fd, int actions, TermiosDarwin* termios);

    public static bool EnterRawMode()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }
        unsafe
        {
            TermiosDarwin current;
            if (TcGetAttr(StdinFileno, &current) != 0)
            {
                return false;
            }
            _saved = current;
            _hasSaved = true;

            current.c_lflag &= ~(DarwinIcanon | DarwinEcho | DarwinEchonl | DarwinIexten);
            // VMIN=0/VTIME=1 turns read() into a ~100 ms polling read so
            // the pump thread can observe cancellation on shutdown
            // (issue #50). See TerminalRawModePolicy for the rationale.
            current.c_cc[DarwinVmin] = TerminalRawModePolicy.Vmin;
            current.c_cc[DarwinVtime] = TerminalRawModePolicy.Vtime;
            return TcSetAttr(StdinFileno, TcsaNow, &current) == 0;
        }
    }

    public static void Restore()
    {
        if (!OperatingSystem.IsMacOS() || !_hasSaved)
        {
            return;
        }
        unsafe
        {
            var copy = _saved;
            TcSetAttr(StdinFileno, TcsaNow, &copy);
        }
        _hasSaved = false;
    }

    /// <summary>
    /// Opens a FileStream over the controlling terminal, bypassing
    /// <see cref="Console.OpenStandardInput"/>. On Unix, .NET wraps stdin
    /// in a managed line-editing reader whenever the input is a tty — that
    /// reader aggregates keystrokes until a newline no matter what the tty
    /// is configured to. Reading <c>/dev/tty</c> directly goes straight
    /// through the libc <c>read()</c> syscall and respects the termios
    /// state set via <see cref="EnterRawMode"/>.
    /// </summary>
    public static Stream OpenTtyReadStream()
    {
        return new FileStream(
            "/dev/tty",
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 1);
    }
}
