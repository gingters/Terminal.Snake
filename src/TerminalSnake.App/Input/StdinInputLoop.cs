namespace TerminalSnake.Input;

// Reads bytes from <paramref name="stream"/> in a loop and turns them into
// InputEvents via BufferedInputParser. The loop's reason for existing is
// issue #44: a bare ESC (0x1B) is ambiguous on a terminal — it might be a
// standalone Escape keypress or the first byte of a CSI/SS3 sequence. The
// parser only commits the bare ESC as <see cref="ConsoleKey.Escape"/> when
// it's told the input has been flushed. Real stdin never naturally raises
// such a signal, so we synthesise it here whenever a read times out (the
// idle window is shorter than any human follow-up keystroke would be) or
// when the stream reaches EOF.
public static class StdinInputLoop
{
    public static async Task RunAsync(
        Stream stream,
        TimeSpan idleTimeout,
        Action<InputEvent> emit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(emit);

        var parser = new BufferedInputParser();
        var buffer = new byte[128];

        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await ReadOnceAsync(stream, buffer, idleTimeout, cancellationToken)
                .ConfigureAwait(false);
            if (read.IsCancelled)
            {
                break;
            }
            if (read.IsEof)
            {
                EmitEvents(parser.Feed(ReadOnlySpan<byte>.Empty, inputFlushed: true), emit);
                break;
            }
            EmitEvents(FeedParser(parser, buffer, read.Count), emit);
        }
    }

    private static IReadOnlyList<InputEvent> FeedParser(
        BufferedInputParser parser, byte[] buffer, int count)
    {
        if (count > 0)
        {
            return parser.Feed(buffer.AsSpan(0, count));
        }
        return parser.Feed(ReadOnlySpan<byte>.Empty, inputFlushed: true);
    }

    private static void EmitEvents(IReadOnlyList<InputEvent> events, Action<InputEvent> emit)
    {
        for (var i = 0; i < events.Count; i++)
        {
            emit(events[i]);
        }
    }

    private static async Task<ReadResult> ReadOnceAsync(
        Stream stream, byte[] buffer, TimeSpan idleTimeout, CancellationToken cancellationToken)
    {
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        idle.CancelAfter(idleTimeout);
        try
        {
            var count = await stream.ReadAsync(buffer.AsMemory(), idle.Token).ConfigureAwait(false);
            return count == 0 ? ReadResult.Eof : ReadResult.Bytes(count);
        }
        catch (OperationCanceledException)
        {
            return cancellationToken.IsCancellationRequested
                ? ReadResult.Cancelled
                : ReadResult.IdleTimeout;
        }
    }

    private readonly record struct ReadResult(int Count, bool IsEof, bool IsCancelled, bool IsIdle)
    {
        public static ReadResult Bytes(int count) => new(count, false, false, false);
        public static ReadResult Eof { get; } = new(0, true, false, false);
        public static ReadResult Cancelled { get; } = new(0, false, true, false);
        public static ReadResult IdleTimeout { get; } = new(0, false, false, true);
    }
}
