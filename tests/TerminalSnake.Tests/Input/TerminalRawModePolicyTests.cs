using TerminalSnake.Input;

namespace TerminalSnake.Tests.Input;

// Issue #50: The pump thread reads stdin from a background task and exits
// when cancellation is requested. With VMIN=1/VTIME=0 the read() syscall
// blocks indefinitely until a byte arrives, so cancellation can only be
// observed after the next keystroke — which is also why shutdown had to
// fall back to a 200 ms magic-number Wait. Setting VTIME to a small
// non-zero value flips the read into a short polling read that wakes
// every VTIME * 100 ms and lets the loop re-check the cancellation
// token.
//
// The actual termios call cannot be exercised in unit tests (it touches
// the controlling tty), so the polling policy is pulled into a pure
// helper and asserted here directly.
public sealed class TerminalRawModePolicyTests
{
    [Fact]
    public void Vmin_is_zero_so_read_returns_after_the_polling_window_even_without_input()
    {
        Assert.Equal(0, TerminalRawModePolicy.Vmin);
    }

    [Fact]
    public void Vtime_is_one_decisecond_so_read_wakes_roughly_every_hundred_milliseconds()
    {
        // termios VTIME is measured in 0.1 s ticks. 1 = ~100 ms, which is
        // short enough for shutdown to feel instant but long enough that
        // the pump thread does not burn the CPU spinning on EAGAIN.
        Assert.Equal(1, TerminalRawModePolicy.Vtime);
    }

    [Fact]
    public void Polling_window_is_under_two_hundred_milliseconds_so_shutdown_does_not_need_a_magic_timeout()
    {
        // Sanity check: whatever VTIME ends up at, it must keep the pump
        // responsive to cancellation faster than the legacy 200 ms Wait
        // that this fix replaces. Otherwise removing the magic timeout
        // would regress shutdown latency.
        var maxWakeMillis = TerminalRawModePolicy.Vtime * 100;
        Assert.InRange(maxWakeMillis, 1, 199);
    }
}
