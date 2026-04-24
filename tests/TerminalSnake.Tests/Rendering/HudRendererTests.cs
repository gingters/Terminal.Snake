using TerminalSnake.Game;
using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Rendering;

public sealed class HudRendererTests
{
    private static (FrameBuffer Buffer, Viewport Viewport) SetupBuffer()
    {
        var viewport = ViewportCalculator.Compute(60, 20, 8);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        return (buffer, viewport);
    }

    [Fact]
    public void Top_row_shows_title_and_level()
    {
        var (buffer, viewport) = SetupBuffer();
        var model = new HudModel(LevelIndex: 3, Mode: GameMode.Player, HelpVisible: false, Strings: HudLocalization.Default);

        new HudRenderer().Render(buffer, viewport, model);

        var topRow = ReadRow(buffer, viewport.TopHudRow);
        Assert.Contains("TerminalSnake", topRow);
        Assert.Contains("Level 3", topRow);
    }

    [Fact]
    public void Demo_indicator_appears_only_in_demo_mode()
    {
        var (buffer, viewport) = SetupBuffer();
        var playerModel = new HudModel(1, GameMode.Player, false, HudLocalization.Default);
        new HudRenderer().Render(buffer, viewport, playerModel);
        Assert.DoesNotContain("Auto-play", ReadRow(buffer, viewport.TopHudRow));

        buffer.Clear();
        var demoModel = playerModel with { Mode = GameMode.Demo };
        new HudRenderer().Render(buffer, viewport, demoModel);
        Assert.Contains("Auto-play", ReadRow(buffer, viewport.TopHudRow));
    }

    [Fact]
    public void Press_h_hint_is_always_visible_in_bottom_row()
    {
        var (buffer, viewport) = SetupBuffer();
        var model = new HudModel(1, GameMode.Player, HelpVisible: false, HudLocalization.Default);
        new HudRenderer().Render(buffer, viewport, model);

        var bottomRow = ReadRow(buffer, viewport.BottomHudRow);
        Assert.Contains("Press H for help", bottomRow);
    }

    [Fact]
    public void Help_legend_only_appears_when_help_is_visible()
    {
        var (buffer, viewport) = SetupBuffer();
        var hidden = new HudModel(1, GameMode.Player, HelpVisible: false, HudLocalization.Default);
        new HudRenderer().Render(buffer, viewport, hidden);
        Assert.DoesNotContain("Tab", ReadRow(buffer, viewport.TopHudRow + 1));
        Assert.DoesNotContain("restart", ReadRow(buffer, viewport.BottomHudRow - 1));

        buffer.Clear();
        var shown = hidden with { HelpVisible = true };
        new HudRenderer().Render(buffer, viewport, shown);
        var topLegend = ReadRow(buffer, viewport.TopHudRow + 1);
        var bottomLegend = ReadRow(buffer, viewport.BottomHudRow - 1);
        Assert.Contains("Tab", topLegend);
        Assert.Contains("Enter", topLegend);
        Assert.Contains("restart", bottomLegend);
        Assert.Contains("toggle", bottomLegend);
        Assert.Contains("Q", bottomLegend);
    }

    [Fact]
    public void Uses_localized_strings()
    {
        var (buffer, viewport) = SetupBuffer();
        var german = HudLocalization.ForLanguageCode("de");
        var model = new HudModel(5, GameMode.Player, HelpVisible: true, Strings: german);

        new HudRenderer().Render(buffer, viewport, model);

        Assert.Contains("H drücken für Hilfe", ReadRow(buffer, viewport.BottomHudRow));
        Assert.Contains("Schlange", ReadRow(buffer, viewport.TopHudRow + 1));
    }

    [Fact]
    public void Rejects_null_buffer_and_null_model()
    {
        var viewport = ViewportCalculator.Compute(60, 20, 8);
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        var model = new HudModel(1, GameMode.Player, false, HudLocalization.Default);

        Assert.Throws<ArgumentNullException>(() => new HudRenderer().Render(null!, viewport, model));
        Assert.Throws<ArgumentNullException>(() => new HudRenderer().Render(buffer, viewport, null!));
    }

    private static string ReadRow(FrameBuffer buffer, int row)
    {
        var chars = new char[buffer.Width];
        for (var x = 0; x < buffer.Width; x++)
        {
            chars[x] = buffer[x, row].Char;
        }
        return new string(chars);
    }
}
