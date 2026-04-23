namespace TerminalSnake.Rendering;

public readonly record struct Viewport(
    int TerminalWidth,
    int TerminalHeight,
    int BoardSide,
    int BoardOriginX,
    int BoardOriginY)
{
    public int BoardCharWidth => BoardSide * ViewportCalculator.CellCharWidth;

    public int BoardCharHeight => BoardSide * ViewportCalculator.CellCharHeight;

    public int FrameOriginX => 0;

    public int FrameOriginY => 0;

    public int TopHudRow => ViewportCalculator.BorderThickness;

    public int BottomHudRow => TerminalHeight - ViewportCalculator.BorderThickness - 1;
}
