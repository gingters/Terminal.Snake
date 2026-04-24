using System.Collections.Immutable;
using TerminalSnake.Domain;

namespace TerminalSnake.Rendering;

public sealed class BoardRenderer
{
    public const char BorderHorizontal = '─';
    public const char BorderVertical = '│';
    public const char BorderCornerTopLeft = '┌';
    public const char BorderCornerTopRight = '┐';
    public const char BorderCornerBottomLeft = '└';
    public const char BorderCornerBottomRight = '┘';
    // Double-line glyphs frame the actual play area so the exit edges are
    // visually distinct from the outer terminal frame (issue #24).
    public const char BoardBorderHorizontal = '═';
    public const char BoardBorderVertical = '║';
    public const char BoardBorderCornerTopLeft = '╔';
    public const char BoardBorderCornerTopRight = '╗';
    public const char BoardBorderCornerBottomLeft = '╚';
    public const char BoardBorderCornerBottomRight = '╝';
    // Double-line box-drawing glyphs paint the snake body as a wide pipe
    // (issue #27). Filling both chars of every 2-char cell — verticals
    // included — gives the body real thickness (two parallel strokes for
    // vertical runs, a solid double rail for horizontals) without going
    // back to an opaque block. Corners pair the turn char with a matching
    // horizontal filler so the spine reaches across to its neighbour.
    public const char BodyHorizontal = '═';
    public const char BodyVertical = '║';
    public const char BodyTurnUpRight = '╚';
    public const char BodyTurnUpLeft = '╝';
    public const char BodyTurnDownRight = '╔';
    public const char BodyTurnDownLeft = '╗';
    // BodyChar is still used to paint orphan overlay cells (the single-cell
    // tail left behind by an exit animation, for example) — those cells have
    // no neighbours to derive a spine shape from, so a plain block is the
    // pragmatic fallback.
    public const char BodyChar = '█';
    public const char EmptyChar = ' ';

    public void Render(
        FrameBuffer buffer,
        Board board,
        Viewport viewport,
        int? selectedSnakeIndex = null,
        IReadOnlyDictionary<Cell, SnakeColor>? overlay = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(board);
        if (board.Size != viewport.BoardSide)
        {
            throw new ArgumentException(
                $"Board size {board.Size} does not match viewport board side {viewport.BoardSide}",
                nameof(viewport));
        }

        buffer.Clear();
        DrawFrame(buffer, viewport);
        DrawBoardBorder(buffer, viewport);
        ClearBoardArea(buffer, viewport);
        for (var i = 0; i < board.Snakes.Length; i++)
        {
            DrawSnake(buffer, board.Snakes[i], viewport, isSelected: selectedSnakeIndex == i);
        }
        DrawOverlay(buffer, viewport, overlay);
    }

    private static void DrawBoardBorder(FrameBuffer buffer, Viewport viewport)
    {
        var topRow = viewport.BoardOriginY - 1;
        var bottomRow = viewport.BoardOriginY + viewport.BoardCharHeight;
        var leftCol = viewport.BoardOriginX - 1;
        var rightCol = viewport.BoardOriginX + viewport.BoardCharWidth;
        TrySetInsideFrame(buffer, leftCol, topRow, BoardBorderCornerTopLeft);
        TrySetInsideFrame(buffer, rightCol, topRow, BoardBorderCornerTopRight);
        TrySetInsideFrame(buffer, leftCol, bottomRow, BoardBorderCornerBottomLeft);
        TrySetInsideFrame(buffer, rightCol, bottomRow, BoardBorderCornerBottomRight);
        for (var x = viewport.BoardOriginX; x < rightCol; x++)
        {
            TrySetInsideFrame(buffer, x, topRow, BoardBorderHorizontal);
            TrySetInsideFrame(buffer, x, bottomRow, BoardBorderHorizontal);
        }
        for (var y = viewport.BoardOriginY; y < bottomRow; y++)
        {
            TrySetInsideFrame(buffer, leftCol, y, BoardBorderVertical);
            TrySetInsideFrame(buffer, rightCol, y, BoardBorderVertical);
        }
    }

    private static void TrySetInsideFrame(FrameBuffer buffer, int x, int y, char ch)
    {
        // At minimum viewport size the board sits flush against the outer
        // terminal frame on the left and right, so the border would overwrite
        // the frame chars and chew a gap in the outer rectangle. Skip any
        // cell that is on the frame edge; the frame already marks the play
        // boundary there.
        if (x <= 0 || x >= buffer.Width - 1 || y <= 0 || y >= buffer.Height - 1)
        {
            return;
        }
        buffer.Set(x, y, ch);
    }

    private static void DrawOverlay(
        FrameBuffer buffer, Viewport viewport, IReadOnlyDictionary<Cell, SnakeColor>? overlay)
    {
        if (overlay is null)
        {
            return;
        }
        foreach (var entry in overlay)
        {
            WriteCell(buffer, viewport, entry.Key, BodyChar, BodyChar, entry.Value, reverse: false);
        }
    }

    private static void DrawFrame(FrameBuffer buffer, Viewport viewport)
    {
        var width = viewport.TerminalWidth;
        var height = viewport.TerminalHeight;
        buffer.Set(0, 0, BorderCornerTopLeft);
        buffer.Set(width - 1, 0, BorderCornerTopRight);
        buffer.Set(0, height - 1, BorderCornerBottomLeft);
        buffer.Set(width - 1, height - 1, BorderCornerBottomRight);
        buffer.DrawHorizontalLine(1, 0, width - 2, BorderHorizontal);
        buffer.DrawHorizontalLine(1, height - 1, width - 2, BorderHorizontal);
        buffer.DrawVerticalLine(0, 1, height - 2, BorderVertical);
        buffer.DrawVerticalLine(width - 1, 1, height - 2, BorderVertical);
    }

    private static void ClearBoardArea(FrameBuffer buffer, Viewport viewport)
    {
        for (var row = 0; row < viewport.BoardCharHeight; row++)
        {
            for (var col = 0; col < viewport.BoardCharWidth; col++)
            {
                buffer.Set(viewport.BoardOriginX + col, viewport.BoardOriginY + row, EmptyChar);
            }
        }
    }

    private static void DrawSnake(FrameBuffer buffer, Snake snake, Viewport viewport, bool isSelected)
    {
        for (var i = 0; i < snake.Segments.Length; i++)
        {
            var segment = snake.Segments[i];
            var (left, right) = ChooseSegmentChars(snake, i, isSelected);
            // Reverse video covers every cell of the selected snake (both
            // the arrow head and the spine body), which flips fg/bg so the
            // whole snake renders as a solid colour pipe against the
            // board instead of a same-coloured line. The shape is still
            // readable because we keep the spine glyphs underneath — the
            // bend structure stays visible through the inverted fill.
            WriteCell(buffer, viewport, segment, left, right, snake.Color, reverse: isSelected);
        }
    }

    private static (char Left, char Right) ChooseSegmentChars(Snake snake, int index, bool isSelected)
    {
        if (index == 0)
        {
            var head = HeadChar(snake.Direction, isSelected);
            return (head, head);
        }
        var (toPrev, toNext) = ConnectionsAt(snake.Segments, index);
        return SpineShapes.TryGetValue((toPrev, toNext), out var shape) ? shape : (BodyChar, BodyChar);
    }

    private static (Direction? ToPrev, Direction? ToNext) ConnectionsAt(
        ImmutableArray<Cell> segments, int index)
    {
        var here = segments[index];
        Direction? toPrev = index > 0 ? DirectionExtensions.FromDelta(here, segments[index - 1]) : null;
        Direction? toNext = index < segments.Length - 1
            ? DirectionExtensions.FromDelta(here, segments[index + 1])
            : null;
        return (toPrev, toNext);
    }

    // Connection-pair → (col 0, col 1) glyphs. The column-0 glyph carries
    // the actual spine; column 1 is the deliberate gap that makes each
    // segment visible as a distinct bead. Turns that continue the spine
    // rightward (┗ ┏) fill column 1 with the horizontal stroke too so the
    // line inside the cell reaches the right-hand neighbour; left-handed
    // turns and pure verticals leave column 1 blank.
    private static readonly IReadOnlyDictionary<(Direction? ToPrev, Direction? ToNext), (char Left, char Right)>
        SpineShapes = new Dictionary<(Direction?, Direction?), (char, char)>
        {
            // Horizontal runs fill both chars for a solid double rail (══).
            // Vertical runs use a single ║ glyph (which itself draws two
            // parallel rails); col 1 stays blank because the double-line
            // corner glyphs only emit one-char-wide down/up legs, and
            // filling col 1 with another ║ would leave that rail dangling
            // above a turn's filler column.
            [(Direction.Up, Direction.Down)] = (BodyVertical, EmptyChar),
            [(Direction.Down, Direction.Up)] = (BodyVertical, EmptyChar),
            [(Direction.Left, Direction.Right)] = (BodyHorizontal, BodyHorizontal),
            [(Direction.Right, Direction.Left)] = (BodyHorizontal, BodyHorizontal),

            // Left-handed turns put the corner in col 0 (═╗ / ═╝ read
            // right-to-left through the cell when entering from the left,
            // but column-wise the corner glyph itself sits at col 0 so
            // that its down/up leg lines up with the vertical body's
            // col-0 ║ on the other side of the bend).
            [(Direction.Left, Direction.Up)] = (BodyTurnUpLeft, EmptyChar),
            [(Direction.Up, Direction.Left)] = (BodyTurnUpLeft, EmptyChar),
            [(Direction.Left, Direction.Down)] = (BodyTurnDownLeft, EmptyChar),
            [(Direction.Down, Direction.Left)] = (BodyTurnDownLeft, EmptyChar),

            // Right-handed turns put the corner in col 0 with a horizontal
            // filler in col 1 so the spine reaches forward to the horizontal
            // neighbour on the right (╔═ / ╚═).
            [(Direction.Right, Direction.Up)] = (BodyTurnUpRight, BodyHorizontal),
            [(Direction.Up, Direction.Right)] = (BodyTurnUpRight, BodyHorizontal),
            [(Direction.Right, Direction.Down)] = (BodyTurnDownRight, BodyHorizontal),
            [(Direction.Down, Direction.Right)] = (BodyTurnDownRight, BodyHorizontal),

            // Tails reuse the straight-run glyphs so they blend into the
            // body; the player reads direction from the head arrow.
            [(Direction.Left, null)] = (BodyHorizontal, BodyHorizontal),
            [(Direction.Right, null)] = (BodyHorizontal, BodyHorizontal),
            [(Direction.Up, null)] = (BodyVertical, EmptyChar),
            [(Direction.Down, null)] = (BodyVertical, EmptyChar),
        };

    // CP437-style pointer heads (► ◄ ▲ ▼) with matching white-pointer
    // outlines for the selection highlight — user call-out on issue #27.
    private static readonly IReadOnlyDictionary<Direction, (char Filled, char Outlined)> HeadCharByDirection =
        new Dictionary<Direction, (char, char)>
        {
            [Direction.Up] = ('▲', '△'),
            [Direction.Down] = ('▼', '▽'),
            [Direction.Left] = ('◄', '◅'),
            [Direction.Right] = ('►', '▻'),
        };

    private static char HeadChar(Direction direction, bool isSelected)
    {
        if (!HeadCharByDirection.TryGetValue(direction, out var chars))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction");
        }
        return isSelected ? chars.Outlined : chars.Filled;
    }

    private static void WriteCell(
        FrameBuffer buffer,
        Viewport viewport,
        Cell segment,
        char chLeft,
        char chRight,
        SnakeColor foreground,
        bool reverse)
    {
        var baseX = viewport.BoardOriginX + segment.X * ViewportCalculator.CellCharWidth;
        var baseY = viewport.BoardOriginY + segment.Y * ViewportCalculator.CellCharHeight;
        buffer.Set(baseX, baseY, chLeft, foreground, background: null, reverse: reverse);
        buffer.Set(baseX + 1, baseY, chRight, foreground, background: null, reverse: reverse);
    }
}
