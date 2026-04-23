using Spectre.Console;
using TerminalSnake.Domain;

namespace TerminalSnake.Rendering;

public static class Theme
{
    public static Color ToSpectre(SnakeColor color) => color switch
    {
        SnakeColor.Red => Color.Red,
        SnakeColor.Cyan => Color.Aqua,
        SnakeColor.Yellow => Color.Yellow,
        SnakeColor.Green => Color.Green,
        SnakeColor.Magenta => Color.Fuchsia,
        SnakeColor.Orange => Color.Orange1,
        SnakeColor.Blue => Color.DodgerBlue1,
        SnakeColor.Lime => Color.Chartreuse1,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown snake color"),
    };

    public static Style BuildStyle(SnakeColor? foreground, SnakeColor? background)
    {
        var fg = foreground is null ? Color.Default : ToSpectre(foreground.Value);
        var bg = background is null ? Color.Default : ToSpectre(background.Value);
        return new Style(foreground: fg, background: bg);
    }
}
