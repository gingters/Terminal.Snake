using TerminalSnake.Input;

namespace TerminalSnake.Tests.Input;

// Drives StdinInputLoop with a controllable in-memory stream so we can
// exercise the idle-flush path without real stdin or real wall-clock waits.
// The loop's contract is: a read that completes with bytes feeds the parser
// without flushing; a read that times out (no input within the idle window)
// flushes the parser; a read that returns 0 (EOF) flushes once and exits.
public sealed class StdinInputLoopTests
{
    [Fact]
    public async Task Bare_escape_emits_escape_after_idle_flush()
    {
        var stream = new ControllableStream();
        var emitted = new List<InputEvent>();
        var idle = TimeSpan.FromMilliseconds(20);
        using var lifetime = new CancellationTokenSource();

        var loop = StdinInputLoop.RunAsync(stream, idle, evt => emitted.Add(evt), lifetime.Token);

        stream.PostBytes(new byte[] { 0x1B });
        await stream.WaitUntilDrainedAsync();
        // No second byte arrives within the idle window -> loop must flush.
        await Task.Delay(idle + TimeSpan.FromMilliseconds(80), TestContext.Current.CancellationToken);

        lifetime.Cancel();
        stream.SignalEof();
        await loop;

        Assert.Contains(new KeyEvent(ConsoleKey.Escape), emitted);
    }

    [Fact]
    public async Task Eof_flushes_pending_bare_escape_before_exit()
    {
        var stream = new ControllableStream();
        var emitted = new List<InputEvent>();
        using var lifetime = new CancellationTokenSource();

        var loop = StdinInputLoop.RunAsync(
            stream, TimeSpan.FromSeconds(5), evt => emitted.Add(evt), lifetime.Token);

        stream.PostBytes(new byte[] { 0x1B });
        await stream.WaitUntilDrainedAsync();
        stream.SignalEof();

        await loop;

        Assert.Contains(new KeyEvent(ConsoleKey.Escape), emitted);
    }

    [Fact]
    public async Task Plain_byte_emits_immediately_without_waiting_for_idle()
    {
        var stream = new ControllableStream();
        var emitted = new List<InputEvent>();
        using var lifetime = new CancellationTokenSource();

        var loop = StdinInputLoop.RunAsync(
            stream, TimeSpan.FromSeconds(5), evt => emitted.Add(evt), lifetime.Token);

        stream.PostBytes(new byte[] { (byte)'q' });
        await stream.WaitUntilDrainedAsync();

        // Give the loop a brief moment to deliver the event; nowhere near
        // the 5 s idle timeout so a passing test means no idle-flush path
        // was needed.
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (emitted.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }

        lifetime.Cancel();
        stream.SignalEof();
        await loop;

        Assert.Equal(new KeyEvent(ConsoleKey.Q), Assert.Single(emitted));
    }
}
