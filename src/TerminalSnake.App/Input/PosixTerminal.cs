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
internal static class PosixTerminal
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

    [DllImport("libc", SetLastError = true)]
    private static extern unsafe int tcgetattr(int fd, TermiosDarwin* termios);

    [DllImport("libc", SetLastError = true)]
    private static extern unsafe int tcsetattr(int fd, int actions, TermiosDarwin* termios);

    public static bool EnterRawMode()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }
        unsafe
        {
            TermiosDarwin current;
            if (tcgetattr(StdinFileno, &current) != 0)
            {
                return false;
            }
            _saved = current;
            _hasSaved = true;

            current.c_lflag &= ~(DarwinIcanon | DarwinEcho | DarwinEchonl | DarwinIexten);
            current.c_cc[DarwinVmin] = 1;
            current.c_cc[DarwinVtime] = 0;
            return tcsetattr(StdinFileno, TcsaNow, &current) == 0;
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
            tcsetattr(StdinFileno, TcsaNow, &copy);
        }
        _hasSaved = false;
    }
}
