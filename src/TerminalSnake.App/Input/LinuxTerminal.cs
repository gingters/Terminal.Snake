using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TerminalSnake.Input;

// Linux counterpart to PosixTerminal. The Darwin and Linux termios
// ABIs are not interchangeable: Linux uses 32-bit tcflag_t (vs Darwin's
// 64-bit), an extra c_line byte before c_cc, NCCS=32 (vs Darwin's 20),
// and the c_lflag bit values + VMIN/VTIME indices differ. Sharing the
// macOS struct on Linux silently corrupted tcgetattr/tcsetattr writes
// and left stdin in canonical mode, so the docker'd Linux build never
// reacted to keystrokes.
//
// Only Linux is implemented; non-Linux callers are no-ops.
[ExcludeFromCodeCoverage]
internal static partial class LinuxTerminal
{
    private const int StdinFileno = 0;
    private const int TcsaNow = 0;

    // Linux termios constants (from <bits/termios-c_lflag.h>).
    private const uint LinuxIcanon = 0x00000002;
    private const uint LinuxEcho   = 0x00000008;
    private const uint LinuxEchonl = 0x00000040;
    private const uint LinuxIexten = 0x00008000;
    private const int LinuxVmin = 6;
    private const int LinuxVtime = 5;

    private const string LibC = "libc";

    private static bool _hasSaved;
    private static TermiosLinux _saved;

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct TermiosLinux
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;
        public fixed byte c_cc[32];
        public uint c_ispeed;
        public uint c_ospeed;
    }

    [LibraryImport(LibC, EntryPoint = "tcgetattr", SetLastError = true)]
    private static unsafe partial int TcGetAttr(int fd, TermiosLinux* termios);

    [LibraryImport(LibC, EntryPoint = "tcsetattr", SetLastError = true)]
    private static unsafe partial int TcSetAttr(int fd, int actions, TermiosLinux* termios);

    public static bool EnterRawMode()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }
        unsafe
        {
            TermiosLinux current;
            if (TcGetAttr(StdinFileno, &current) != 0)
            {
                return false;
            }
            _saved = current;
            _hasSaved = true;

            current.c_lflag &= ~(LinuxIcanon | LinuxEcho | LinuxEchonl | LinuxIexten);
            current.c_cc[LinuxVmin] = 1;
            current.c_cc[LinuxVtime] = 0;
            return TcSetAttr(StdinFileno, TcsaNow, &current) == 0;
        }
    }

    public static void Restore()
    {
        if (!OperatingSystem.IsLinux() || !_hasSaved)
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
}
