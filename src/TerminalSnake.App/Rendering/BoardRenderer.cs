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
    public const char BodyChar = '█';
    public const char SelectedBodyChar = '▓';
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
        ClearBoardArea(buffer, viewport);
        for (var i = 0; i < board.Snakes.Length; i++)
        {
            DrawSnake(buffer, board.Snakes[i], viewport, isSelected: selectedSnakeIndex == i);
        }
        DrawOverlay(buffer, viewport, overlay);
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
            WriteCell(buffer, viewport, entry.Key, BodyChar, entry.Value, reverse: false);
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
            var isHead = i == 0;
            var ch = ChooseSegmentChar(snake.Direction, isHead, isSelected);
            // Layer three orthogonal cues for the selection so at least one
            // still reads even on terminals that ignore reverse video:
            //   1) Head switches to an outlined arrow (shape).
            //   2) Body switches from full to shaded block (texture).
            //   3) Head gets reverse video on top (color swap).
            var reverse = isHead && isSelected;
            WriteCell(buffer, viewport, segment, ch, snake.Color, reverse);
        }
    }

    private static char ChooseSegmentChar(Direction direction, bool isHead, bool isSelected)
    {
        if (isHead)
        {
            return HeadChar(direction, isSelected);
        }
        return isSelected ? SelectedBodyChar : BodyChar;
    }

    private static readonly IReadOnlyDictionary<Direction, (char Filled, char Outlined)> HeadCharByDirection =
        new Dictionary<Direction, (char, char)>
        {
            [Direction.Up] = ('▲', '△'),
            [Direction.Down] = ('▼', '▽'),
            [Direction.Left] = ('◀', '◁'),
            [Direction.Right] = ('▶', '▷'),
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
        char ch,
        SnakeColor foreground,
        bool reverse)
    {
        var baseX = viewport.BoardOriginX + segment.X * ViewportCalculator.CellCharWidth;
        var baseY = viewport.BoardOriginY + segment.Y * ViewportCalculator.CellCharHeight;
        for (var dx = 0; dx < ViewportCalculator.CellCharWidth; dx++)
        {
            buffer.Set(baseX + dx, baseY, ch, foreground, background: null, reverse: reverse);
        }
    }
}
