using Spectre.Console;
using TerminalSnake.Domain;
using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Rendering;

public sealed class ThemeTests
{
    [Fact]
    public void ToSpectre_returns_distinct_color_per_snake_color()
    {
        var allSnakeColors = Enum.GetValues<SnakeColor>();
        var mappedColors = allSnakeColors.Select(Theme.ToSpectre).ToArray();
        Assert.Equal(allSnakeColors.Length, mappedColors.Distinct().Count());
    }

    [Fact]
    public void ToSpectre_rejects_invalid_enum_value()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Theme.ToSpectre((SnakeColor)999));
    }

    [Fact]
    public void BuildStyle_with_nulls_yields_default_colors()
    {
        var style = Theme.BuildStyle(null, null);
        Assert.Equal(Color.Default, style.Foreground);
        Assert.Equal(Color.Default, style.Background);
    }

    [Fact]
    public void BuildStyle_maps_foreground_and_background()
    {
        var style = Theme.BuildStyle(SnakeColor.Red, SnakeColor.Cyan);
        Assert.Equal(Theme.ToSpectre(SnakeColor.Red), style.Foreground);
        Assert.Equal(Theme.ToSpectre(SnakeColor.Cyan), style.Background);
    }

    [Fact]
    public void BuildStyle_with_reverse_applies_invert_decoration()
    {
        var style = Theme.BuildStyle(SnakeColor.Red, null, reverse: true);
        Assert.True(style.Decoration.HasFlag(Decoration.Invert));
    }

    [Fact]
    public void BuildStyle_without_reverse_has_no_invert_decoration()
    {
        var style = Theme.BuildStyle(SnakeColor.Red, null);
        Assert.False(style.Decoration.HasFlag(Decoration.Invert));
    }
}
