namespace TerminalSnake.Rendering;

public static class ViewportCalculator
{
    public const int MinBoardSide = 5;
    public const int BorderThickness = 1;
    public const int PaddingLinesPerSide = 2;
    public const int CellCharWidth = 2;
    public const int CellCharHeight = 1;

    public static Viewport Compute(int terminalWidth, int terminalHeight, int boardSide)
    {
        EnsureBoardSide(boardSide);
        var (requiredWidth, requiredHeight) = RequiredTerminalSize(boardSide);
        EnsureTerminalFits(requiredWidth, requiredHeight, terminalWidth, terminalHeight);

        var boardCharWidth = boardSide * CellCharWidth;
        var boardCharHeight = boardSide * CellCharHeight;
        var boardOriginX = (terminalWidth - boardCharWidth) / 2;
        var innerHeight = terminalHeight - 2 * BorderThickness - 2 * PaddingLinesPerSide;
        var verticalSlack = innerHeight - boardCharHeight;
        var boardOriginY = BorderThickness + PaddingLinesPerSide + verticalSlack / 2;

        return new Viewport(terminalWidth, terminalHeight, boardSide, boardOriginX, boardOriginY);
    }

    public static int MaxBoardSide(int terminalWidth, int terminalHeight)
    {
        if (terminalWidth < MinimumWidth || terminalHeight < MinimumHeight)
        {
            return 0;
        }
        var maxByWidth = (terminalWidth - 2 * BorderThickness) / CellCharWidth;
        var maxByHeight = terminalHeight - 2 * BorderThickness - 2 * PaddingLinesPerSide;
        return Math.Max(0, Math.Min(maxByWidth, maxByHeight));
    }

    public static int MinimumWidth => MinBoardSide * CellCharWidth + 2 * BorderThickness;

    public static int MinimumHeight => MinBoardSide + 2 * BorderThickness + 2 * PaddingLinesPerSide;

    private static void EnsureBoardSide(int boardSide)
    {
        if (boardSide < MinBoardSide)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boardSide), boardSide, $"Board side must be at least {MinBoardSide}");
        }
    }

    private static (int Width, int Height) RequiredTerminalSize(int boardSide)
    {
        var width = boardSide * CellCharWidth + 2 * BorderThickness;
        var height = boardSide * CellCharHeight + 2 * BorderThickness + 2 * PaddingLinesPerSide;
        return (width, height);
    }

    private static void EnsureTerminalFits(
        int requiredWidth, int requiredHeight, int terminalWidth, int terminalHeight)
    {
        if (terminalWidth < requiredWidth || terminalHeight < requiredHeight)
        {
            throw new TerminalTooSmallException(
                requiredWidth, requiredHeight, terminalWidth, terminalHeight);
        }
    }
}
