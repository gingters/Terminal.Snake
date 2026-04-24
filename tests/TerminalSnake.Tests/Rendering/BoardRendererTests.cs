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
        Assert.Equal('►', buffer[headBaseX, headBaseY].Char);
        Assert.Equal('►', buffer[headBaseX + 1, headBaseY].Char);
    }

    [Fact]
    public void Horizontal_body_paints_the_spine_glyph_in_the_snake_color()
    {
        // Issue #27: a horizontal body cell flanked by other horizontal
        // cells draws the double horizontal spine glyph '═' at both cols
        // so the body reads as a thick pipe rather than an opaque block.
        var board = SimpleBoard(6, Snake(SnakeColor.Cyan, (3, 2), (2, 2), (1, 2)));
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport);

        var bodyBaseX = viewport.BoardOriginX + 2 * ViewportCalculator.CellCharWidth;
        var bodyBaseY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.Equal(BoardRenderer.BodyHorizontal, buffer[bodyBaseX, bodyBaseY].Char);
        Assert.Equal(BoardRenderer.BodyHorizontal, buffer[bodyBaseX + 1, bodyBaseY].Char);
        Assert.Equal(SnakeColor.Cyan, buffer[bodyBaseX, bodyBaseY].Foreground);
    }

    [Fact]
    public void Selected_snake_keeps_its_spine_glyphs_but_applies_reverse_video_everywhere()
    {
        var snake = Snake(SnakeColor.Yellow, (2, 2), (1, 2), (0, 2));
        var board = SimpleBoard(6, snake);
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport, selectedSnakeIndex: 0);

        var headX = viewport.BoardOriginX + 2 * ViewportCalculator.CellCharWidth;
        var headY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        // Outlined pointer (shape cue) — distinct from the filled '►' of an unselected snake.
        Assert.Equal('▻', buffer[headX, headY].Char);
        Assert.True(buffer[headX, headY].Reverse, "selected snake's head should be in reverse video");
        Assert.Equal(SnakeColor.Yellow, buffer[headX, headY].Foreground);

        // Body cell also renders as a spine glyph — crucially, with
        // Reverse=true so the whole snake shows up as a solid colour pipe
        // rather than a same-coloured line.
        var bodyX = viewport.BoardOriginX + 1 * ViewportCalculator.CellCharWidth;
        var bodyY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.True(buffer[bodyX, bodyY].Reverse, "selected snake's body should be in reverse video");
        Assert.Equal(BoardRenderer.BodyHorizontal, buffer[bodyX, bodyY].Char);
    }

    [Fact]
    public void Unselected_snake_uses_filled_arrow_head_and_spine_body()
    {
        var snake = Snake(SnakeColor.Yellow, (2, 2), (1, 2), (0, 2));
        var board = SimpleBoard(6, snake);
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport, selectedSnakeIndex: null);

        var headX = viewport.BoardOriginX + 2 * ViewportCalculator.CellCharWidth;
        var headY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.Equal('►', buffer[headX, headY].Char);
        Assert.False(buffer[headX, headY].Reverse);

        var bodyX = viewport.BoardOriginX + 1 * ViewportCalculator.CellCharWidth;
        var bodyY = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
        Assert.Equal(BoardRenderer.BodyHorizontal, buffer[bodyX, bodyY].Char);
    }

    [Theory]
    [InlineData(Direction.Up, '▲', '△')]
    [InlineData(Direction.Down, '▼', '▽')]
    [InlineData(Direction.Left, '◄', '◅')]
    [InlineData(Direction.Right, '►', '▻')]
    public void Head_char_outline_differs_between_unselected_and_selected(
        Direction direction, char unselected, char selected)
    {
        var head = new Cell(3, 3);
        var tail = head + direction.Opposite().Delta();
        var snake = new Snake(new[] { head, tail }, SnakeColor.Red);
        var board = SimpleBoard(7, snake);
        var viewport = ViewportCalculator.Compute(40, 16, 7);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);

        new BoardRenderer().Render(buffer, board, viewport, selectedSnakeIndex: null);
        var headX = viewport.BoardOriginX + head.X * ViewportCalculator.CellCharWidth;
        var headY = viewport.BoardOriginY + head.Y * ViewportCalculator.CellCharHeight;
        Assert.Equal(unselected, buffer[headX, headY].Char);

        new BoardRenderer().Render(buffer, board, viewport, selectedSnakeIndex: 0);
        Assert.Equal(selected, buffer[headX, headY].Char);
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
        Assert.Contains("►", snapshot);
        Assert.Contains(BoardRenderer.BodyHorizontal.ToString(), snapshot);
    }

    [Fact]
    public void Board_border_is_drawn_around_the_play_area_when_viewport_has_slack()
    {
        // Issue #24: the outer terminal frame is several cells away from the
        // play area on spacious terminals, so the actual edge where snakes
        // exit needs its own visible border. With a taller/wider viewport
        // than the minimum, the double-line box lands in padding space.
        var board = SimpleBoard(6, Snake(SnakeColor.Red, (0, 0), (1, 0)));
        var viewport = ViewportCalculator.Compute(60, 20, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport);

        var topRow = viewport.BoardOriginY - 1;
        var bottomRow = viewport.BoardOriginY + viewport.BoardCharHeight;
        var leftCol = viewport.BoardOriginX - 1;
        var rightCol = viewport.BoardOriginX + viewport.BoardCharWidth;
        Assert.Equal(BoardRenderer.BoardBorderCornerTopLeft, buffer[leftCol, topRow].Char);
        Assert.Equal(BoardRenderer.BoardBorderCornerTopRight, buffer[rightCol, topRow].Char);
        Assert.Equal(BoardRenderer.BoardBorderCornerBottomLeft, buffer[leftCol, bottomRow].Char);
        Assert.Equal(BoardRenderer.BoardBorderCornerBottomRight, buffer[rightCol, bottomRow].Char);
        Assert.Equal(BoardRenderer.BoardBorderHorizontal, buffer[viewport.BoardOriginX + 2, topRow].Char);
        Assert.Equal(BoardRenderer.BoardBorderVertical, buffer[leftCol, viewport.BoardOriginY + 2].Char);
    }

    [Fact]
    public void Board_border_never_overwrites_the_outer_terminal_frame()
    {
        // At minimum viewport size the board is flush against the outer
        // frame on the left and right. The board border must not eat into
        // the frame glyphs — those are what the player sees as the left/
        // right play-area edge in that layout.
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
        Assert.Equal(BoardRenderer.BorderVertical, buffer[0, viewport.BoardOriginY].Char);
        Assert.Equal(BoardRenderer.BorderVertical,
            buffer[viewport.TerminalWidth - 1, viewport.BoardOriginY].Char);
    }

    [Fact]
    public void Body_traces_its_spine_through_verticals_corners_and_a_tail()
    {
        // Issue #27: the body is drawn as a double-line pipe with
        // neighbour-aware corner glyphs instead of a run of opaque blocks.
        // This snake heads right at (4,2), turns down at (3,2)→(3,4),
        // turns right again at (4,4)→(5,4). Each body cell should pick up
        // the (col0, col1) glyph pair appropriate to its connections.
        var snake = Snake(
            SnakeColor.Red,
            (4, 2),  // head — Direction inferred as Right from seg[1]=(3,2)
            (3, 2),  // turn: connects Right (to head) and Down (to next) → ╔═
            (3, 3),  // pure vertical body → ║║
            (3, 4),  // turn: connects Up (to prev) and Right (to next) → ╚═
            (4, 4),  // horizontal body → ══
            (5, 4)); // tail — prev is Left, no next → ══
        var board = SimpleBoard(7, snake);
        var viewport = ViewportCalculator.Compute(40, 14, 7);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport);

        char CharAt(int boardX, int boardY, int subCol = 0)
        {
            var px = viewport.BoardOriginX + boardX * ViewportCalculator.CellCharWidth + subCol;
            var py = viewport.BoardOriginY + boardY * ViewportCalculator.CellCharHeight;
            return buffer[px, py].Char;
        }

        // Head direction pointer fills both chars of the cell.
        Assert.Equal('►', CharAt(4, 2));
        Assert.Equal('►', CharAt(4, 2, subCol: 1));
        // Upper turn (3,2): connects Right + Down → ╔ at col 0, ═ at col 1.
        Assert.Equal(BoardRenderer.BodyTurnDownRight, CharAt(3, 2));
        Assert.Equal(BoardRenderer.BodyHorizontal, CharAt(3, 2, subCol: 1));
        // Vertical body: ║ at col 0 (the double-vertical glyph already
        // draws two parallel rails inside its one char), col 1 blank so
        // the corner chars' one-wide down/up legs align cleanly.
        Assert.Equal(BoardRenderer.BodyVertical, CharAt(3, 3));
        Assert.Equal(BoardRenderer.EmptyChar, CharAt(3, 3, subCol: 1));
        // Lower turn (3,4): connects Up + Right → ╚ at col 0, ═ at col 1.
        Assert.Equal(BoardRenderer.BodyTurnUpRight, CharAt(3, 4));
        Assert.Equal(BoardRenderer.BodyHorizontal, CharAt(3, 4, subCol: 1));
        // Horizontal body at (4,4) — ══.
        Assert.Equal(BoardRenderer.BodyHorizontal, CharAt(4, 4));
        Assert.Equal(BoardRenderer.BodyHorizontal, CharAt(4, 4, subCol: 1));
        // Tail at (5,4) reuses the horizontal glyph — head arrow carries
        // the directional cue, tail just blends into the body.
        Assert.Equal(BoardRenderer.BodyHorizontal, CharAt(5, 4));
        Assert.Equal(BoardRenderer.BodyHorizontal, CharAt(5, 4, subCol: 1));
    }

    [Fact]
    public void Left_handed_turns_place_the_corner_glyph_in_col_0_to_align_with_vertical_neighbour()
    {
        // A snake turning from a Left-going horizontal into a Down or Up
        // exit puts the ╗/╝ glyph in col 0 so its down/up leg lines up
        // with col 0 of the vertical body on the other side of the bend
        // (the vertical body itself lives in col 0). Col 1 stays blank.
        var snake = Snake(
            SnakeColor.Green,
            (0, 4),  // head — Direction is Left (seg[1] is to the right)
            (1, 4),  // horizontal body
            (2, 4),  // turn: connects Left (toward prev seg 1) and Up (toward next) → ╝
            (2, 3),  // vertical
            (2, 2)); // tail
        var board = SimpleBoard(7, snake);
        var viewport = ViewportCalculator.Compute(40, 14, 7);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport);

        var turnLeft = viewport.BoardOriginX + 2 * ViewportCalculator.CellCharWidth;
        var turnRow = viewport.BoardOriginY + 4;
        Assert.Equal(BoardRenderer.BodyTurnUpLeft, buffer[turnLeft, turnRow].Char);
        Assert.Equal(BoardRenderer.EmptyChar, buffer[turnLeft + 1, turnRow].Char);
    }

    [Fact]
    public void Every_cell_of_the_selected_snake_is_rendered_with_reverse_video()
    {
        // The selection highlight is reverse video applied uniformly across
        // the whole snake (head + every body cell), so the snake reads as
        // a solid colour pipe rather than a same-coloured line. The chars
        // themselves stay the spine glyphs so the bend shape is still
        // visible through the inversion.
        var snake = Snake(SnakeColor.Yellow, (4, 2), (3, 2), (2, 2), (1, 2));
        var board = SimpleBoard(6, snake);
        var viewport = ViewportCalculator.Compute(40, 12, 6);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        new BoardRenderer().Render(buffer, board, viewport, selectedSnakeIndex: 0);

        for (var i = 0; i < 4; i++)
        {
            var px = viewport.BoardOriginX + (4 - i) * ViewportCalculator.CellCharWidth;
            var py = viewport.BoardOriginY + 2 * ViewportCalculator.CellCharHeight;
            Assert.True(
                buffer[px, py].Reverse,
                $"selected snake cell at board ({4 - i},2) should have reverse video applied");
        }
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
