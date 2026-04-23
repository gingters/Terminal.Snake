using TerminalSnake.Domain;
using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Rendering;

public sealed class FrameBufferTests
{
    [Fact]
    public void Constructor_rejects_non_positive_dimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameBuffer(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameBuffer(5, -1));
    }

    [Fact]
    public void Cleared_buffer_is_filled_with_spaces()
    {
        var buffer = new FrameBuffer(3, 2);
        var snapshot = buffer.Snapshot();
        Assert.Equal("   \n   ", snapshot);
    }

    [Fact]
    public void Set_and_indexer_write_and_read_same_glyph()
    {
        var buffer = new FrameBuffer(5, 5);
        buffer.Set(2, 3, 'X', SnakeColor.Red, SnakeColor.Blue);
        var glyph = buffer[2, 3];
        Assert.Equal('X', glyph.Char);
        Assert.Equal(SnakeColor.Red, glyph.Foreground);
        Assert.Equal(SnakeColor.Blue, glyph.Background);
    }

    [Fact]
    public void DrawHorizontalLine_sets_contiguous_chars()
    {
        var buffer = new FrameBuffer(6, 3);
        buffer.DrawHorizontalLine(1, 1, 4, '-');
        Assert.Equal("      \n ---- \n      ", buffer.Snapshot());
    }

    [Fact]
    public void DrawVerticalLine_sets_contiguous_chars()
    {
        var buffer = new FrameBuffer(4, 4);
        buffer.DrawVerticalLine(1, 0, 3, '|');
        Assert.Equal(" |  \n |  \n |  \n    ", buffer.Snapshot());
    }

    [Fact]
    public void Out_of_bounds_access_throws()
    {
        var buffer = new FrameBuffer(3, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Set(3, 0, 'x'));
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = buffer[0, 3]; });
    }

    [Fact]
    public void Clear_with_fill_replaces_all_glyphs()
    {
        var buffer = new FrameBuffer(3, 2);
        buffer.Clear('.', SnakeColor.Green);
        Assert.Equal("...\n...", buffer.Snapshot());
        Assert.Equal(SnakeColor.Green, buffer[1, 1].Foreground);
    }
}
