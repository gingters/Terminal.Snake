using TerminalSnake.Domain;
using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Rendering;

public sealed class BoardRendererTests
{
    private static Board SimpleBoard(int size, params Snake[] snakes) => new(size, snakes);

    private static Snake Snake(SnakeColor color, params (int X, int Y)[] cells) =>
        new(cells.Select(c => new Cell(c.X, c.Y)), color);

    [Fact]
    public void Renders_frame_corners_and_sides()
    {
        var board = SimpleBoard(5, Snake(SnakeColor.Red, (0, 0), (1, 0)));
        var viewport = ViewportCalculator.Compute(
            ViewportCalculator.MinimumWidth,
            ViewportCalculator.MinimumHeight,
            5);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport);

        Assert.Equal(BoardRenderer.BorderCornerTopLeft, buffer[0, 0].Char);
        Assert.Equal(BoardRenderer.BorderCornerTopRight, buffer[viewport.TerminalWidth - 1, 0].Char);
        Assert.Equal(BoardRenderer.BorderCornerBottomLeft, buffer[0, viewport.TerminalHeight - 1].Char);
        Assert.Equal(BoardRenderer.BorderCornerBottomRight,
            buffer[viewport.TerminalWidth - 1, viewport.TerminalHeight - 1].Char);
        Assert.Equal(BoardRenderer.BorderHorizontal, buffer[3, 0].Char);
        Assert.Equal(BoardRenderer.BorderVertical, buffer[0, 3].Char);
    }

    [Fact]
    public void Head_char_reflects_direction()
    {
        var board = SimpleBoard(6, Snake(SnakeColor.Red, (3, 2), (2, 2)));
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport);

        var headBaseX = viewport.BoardOriginX + 3 * ViewportCalculator.CellCharWidth;
        var headBaseY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.Equal('▶', buffer[headBaseX, headBaseY].Char);
        Assert.Equal('▶', buffer[headBaseX + 1, headBaseY].Char);
    }

    [Fact]
    public void Body_uses_block_char_and_snake_color()
    {
        var board = SimpleBoard(6, Snake(SnakeColor.Cyan, (3, 2), (2, 2), (1, 2)));
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport);

        var bodyBaseX = viewport.BoardOriginX + 2 * ViewportCalculator.CellCharWidth;
        var bodyBaseY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.Equal(BoardRenderer.BodyChar, buffer[bodyBaseX, bodyBaseY].Char);
        Assert.Equal(SnakeColor.Cyan, buffer[bodyBaseX, bodyBaseY].Foreground);
    }

    [Fact]
    public void Selected_snake_head_uses_reverse_video_and_body_uses_shaded_block()
    {
        var snake = Snake(SnakeColor.Yellow, (2, 2), (1, 2), (0, 2));
        var board = SimpleBoard(6, snake);
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport, selectedSnakeIndex: 0);

        var headX = viewport.BoardOriginX + 2 * ViewportCalculator.CellCharWidth;
        var headY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.True(buffer[headX, headY].Reverse, "selected snake's head should be rendered in reverse video");
        Assert.Equal(SnakeColor.Yellow, buffer[headX, headY].Foreground);

        var bodyX = viewport.BoardOriginX + 1 * ViewportCalculator.CellCharWidth;
        var bodyY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.False(buffer[bodyX, bodyY].Reverse);
        Assert.Equal(BoardRenderer.SelectedBodyChar, buffer[bodyX, bodyY].Char);
    }

    [Fact]
    public void Unselected_snake_head_is_not_reverse_and_body_uses_full_block()
    {
        var snake = Snake(SnakeColor.Yellow, (2, 2), (1, 2), (0, 2));
        var board = SimpleBoard(6, snake);
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport, selectedSnakeIndex: null);

        var headX = viewport.BoardOriginX + 2 * ViewportCalculator.CellCharWidth;
        var headY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.False(buffer[headX, headY].Reverse);

        var bodyX = viewport.BoardOriginX + 1 * ViewportCalculator.CellCharWidth;
        var bodyY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.Equal(BoardRenderer.BodyChar, buffer[bodyX, bodyY].Char);
    }

    [Fact]
    public void Throws_when_board_size_mismatches_viewport()
    {
        var board = SimpleBoard(5, Snake(SnakeColor.Red, (0, 0), (1, 0)));
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        Assert.Throws<ArgumentException>(() => new BoardRenderer().Render(buffer, board, viewport));
    }

    [Fact]
    public void Null_arguments_are_rejected()
    {
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        var board = SimpleBoard(6, Snake(SnakeColor.Red, (0, 0), (1, 0)));
        Assert.Throws<ArgumentNullException>(() => new BoardRenderer().Render(null!, board, viewport));
        Assert.Throws<ArgumentNullException>(() => new BoardRenderer().Render(buffer, null!, viewport));
    }

    [Fact]
    public void Snapshot_reflects_drawn_snake_shape_on_small_board()
    {
        var board = SimpleBoard(5, Snake(SnakeColor.Red, (2, 2), (1, 2), (0, 2)));
        var viewport = ViewportCalculator.Compute(
            ViewportCalculator.MinimumWidth,
            ViewportCalculator.MinimumHeight,
            5);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport);

        var snapshot = buffer.Snapshot();
        Assert.Contains("▶", snapshot);
        Assert.Contains("█", snapshot);
    }

    [Fact]
    public void Overlay_cells_are_painted_as_body_blocks_in_their_color()
    {
        var board = SimpleBoard(6, Snake(SnakeColor.Red, (0, 0), (1, 0)));
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        var overlay = new Dictionary<Cell, SnakeColor>
        {
            [new Cell(4, 4)] = SnakeColor.Green,
        };
        new BoardRenderer().Render(buffer, board, viewport, overlay: overlay);

        var overlayBaseX = viewport.BoardOriginX + 4 * ViewportCalculator.CellCharWidth;
        var overlayBaseY = viewport.BoardOriginY + 4 * ViewportCalculator.CellCharHeight;
        Assert.Equal(BoardRenderer.BodyChar, buffer[overlayBaseX, overlayBaseY].Char);
        Assert.Equal(SnakeColor.Green, buffer[overlayBaseX, overlayBaseY].Foreground);
    }
}
