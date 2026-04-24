using TerminalSnake.Domain;
using TerminalSnake.Game;

namespace TerminalSnake.Rendering;

public sealed record HudModel(int LevelIndex, GameMode Mode, bool HelpVisible, HudStrings Strings);

public sealed class HudRenderer
{
    public void Render(FrameBuffer buffer, Viewport viewport, HudModel model)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(model);
        DrawTopBar(buffer, viewport, model);
        DrawPressHForHelp(buffer, viewport, model);
        if (model.HelpVisible)
        {
            DrawHelpLegend(buffer, viewport, model);
        }
    }

    private static void DrawTopBar(FrameBuffer buffer, Viewport viewport, HudModel model)
    {
        var titleRow = viewport.TopHudRow;
        var titleText = $" {model.Strings.Title}  {model.Strings.LevelLabel} {model.LevelIndex}";
        WriteText(buffer, x: 1, y: titleRow, titleText);
        if (model.Mode == GameMode.Demo)
        {
            var demo = model.Strings.DemoIndicator;
            var demoX = Math.Max(1, viewport.TerminalWidth - demo.Length - 2);
            WriteText(buffer, demoX, titleRow, demo);
        }
    }

    private static void DrawPressHForHelp(FrameBuffer buffer, Viewport viewport, HudModel model)
    {
        var hint = model.Strings.PressHForHelp;
        var row = viewport.BottomHudRow;
        var x = Math.Max(1, viewport.TerminalWidth - hint.Length - 2);
        WriteText(buffer, x, row, hint);
    }

    private static void DrawHelpLegend(FrameBuffer buffer, Viewport viewport, HudModel model)
    {
        // Split the legend across the two padding rows that frame the
        // board — the full text does not fit on narrower terminals and
        // would get clipped mid-item otherwise. Top padding's second row
        // carries the selection + release shortcuts; the row just above
        // "Press H for help" carries the remaining three.
        var strings = model.Strings;
        var selectionReleaseLine = $"{strings.HelpTab}   {strings.HelpEnter}";
        var utilityLine = $"{strings.HelpR}   {strings.HelpH}   {strings.HelpQ}";
        WriteText(buffer, x: 1, y: viewport.TopHudRow + 1, selectionReleaseLine);
        WriteText(buffer, x: 1, y: viewport.BottomHudRow - 1, utilityLine);
    }

    private static void WriteText(FrameBuffer buffer, int x, int y, string text)
    {
        if (y < 0 || y >= buffer.Height)
        {
            return;
        }
        var maxWidth = buffer.Width - x - 1;
        if (maxWidth <= 0)
        {
            return;
        }
        var clipped = text.Length > maxWidth ? text[..maxWidth] : text;
        for (var i = 0; i < clipped.Length; i++)
        {
            buffer.Set(x + i, y, clipped[i]);
        }
    }
}
