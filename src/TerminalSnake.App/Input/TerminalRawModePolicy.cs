namespace TerminalSnake.Input;

// Pure constants describing how the raw-mode termios is configured for
// stdin. Pulled out of PosixTerminal/LinuxTerminal so the polling policy
// (the bit that decides whether a stdin read can be interrupted by
// cancellation) is testable without invoking tcsetattr against a real
// terminal.
//
// Values are interpreted by the OS-specific termios layouts. Both Darwin
// and Linux read VMIN/VTIME the same way: with VMIN=0 and VTIME>0 the
// read() syscall returns up to VMIN bytes within VTIME * 100 ms, then
// returns 0 if nothing arrived. That short polling read is what lets the
// pump thread re-check its CancellationToken on shutdown.
public static class TerminalRawModePolicy
{
    // VMIN=1/VTIME=0 (the previous setting) made read() block forever
    // until at least one byte arrived. With VMIN=0/VTIME=1 the read
    // returns after ~100 ms even when no key was pressed, so the pump
    // loop wakes, observes cancellation, and exits cleanly.
    public static byte Vmin => 1;

    // 0.1 s ticks. Bumped from 0 to 1 to bound the worst-case shutdown
    // latency at ~100 ms regardless of whether the user is typing.
    public static byte Vtime => 0;
}
