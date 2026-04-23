using System.Text;
using TerminalSnake.Input;

namespace TerminalSnake.Tests.Input;

public sealed class BufferedInputParserTests
{
    [Fact]
    public void Empty_feed_returns_no_events()
    {
        var parser = new BufferedInputParser();
        var events = parser.Feed(ReadOnlySpan<byte>.Empty);
        Assert.Empty(events);
    }

    [Fact]
    public void Plain_ascii_bytes_produce_immediate_key_events()
    {
        var parser = new BufferedInputParser();
        var events = parser.Feed(new[] { (byte)'q', (byte)'r' });
        Assert.Equal(2, events.Count);
        Assert.Equal(new KeyEvent(ConsoleKey.Q), events[0]);
        Assert.Equal(new KeyEvent(ConsoleKey.R), events[1]);
    }

    [Fact]
    public void Escape_sequence_split_across_two_feeds_still_parses()
    {
        var parser = new BufferedInputParser();
        var first = parser.Feed(new[] { (byte)0x1B, (byte)'[' });
        Assert.Empty(first);
        Assert.Equal(2, parser.PendingBytes);

        var second = parser.Feed(new[] { (byte)'A' });
        Assert.Single(second);
        Assert.Equal(new KeyEvent(ConsoleKey.UpArrow), second[0]);
        Assert.Equal(0, parser.PendingBytes);
    }

    [Fact]
    public void Mouse_event_split_across_feeds_still_parses()
    {
        var parser = new BufferedInputParser();
        var first = parser.Feed(Encoding.ASCII.GetBytes("\x1b[<0;"));
        Assert.Empty(first);

        var second = parser.Feed(Encoding.ASCII.GetBytes("12;7M"));
        Assert.Single(second);
        var click = Assert.IsType<MouseClickEvent>(second[0]);
        Assert.Equal(11, click.Column);
        Assert.Equal(6, click.Row);
    }

    [Fact]
    public void Reset_clears_pending_bytes()
    {
        var parser = new BufferedInputParser();
        parser.Feed(new[] { (byte)0x1B, (byte)'[' });
        Assert.Equal(2, parser.PendingBytes);
        parser.Reset();
        Assert.Equal(0, parser.PendingBytes);
    }

    [Fact]
    public void Flush_commits_bare_escape_as_escape_key()
    {
        var parser = new BufferedInputParser();
        var buffered = parser.Feed(new[] { (byte)0x1B });
        Assert.Empty(buffered);

        var flushed = parser.Feed(ReadOnlySpan<byte>.Empty, inputFlushed: true);
        Assert.Single(flushed);
        Assert.Equal(new KeyEvent(ConsoleKey.Escape), flushed[0]);
    }

    [Fact]
    public void Multiple_events_in_one_feed_are_all_emitted()
    {
        var parser = new BufferedInputParser();
        var buffer = new List<byte>();
        buffer.AddRange("q\t"u8.ToArray());
        buffer.AddRange(Encoding.ASCII.GetBytes("\x1b[A"));
        buffer.AddRange("r"u8.ToArray());
        var events = parser.Feed(buffer.ToArray());
        Assert.Equal(4, events.Count);
        Assert.Equal(ConsoleKey.Q, ((KeyEvent)events[0]).Key);
        Assert.Equal(ConsoleKey.Tab, ((KeyEvent)events[1]).Key);
        Assert.Equal(ConsoleKey.UpArrow, ((KeyEvent)events[2]).Key);
        Assert.Equal(ConsoleKey.R, ((KeyEvent)events[3]).Key);
    }
}
