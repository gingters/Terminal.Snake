using TerminalSnake.Input;

namespace TerminalSnake.Tests.Input;

public sealed class TerminalEscapeSequencesTests
{
    [Fact]
    public void Enable_mouse_includes_sgr_and_any_event_flags()
    {
        Assert.Contains("1000h", TerminalEscapeSequences.EnableMouse);
        Assert.Contains("1006h", TerminalEscapeSequences.EnableMouse);
    }

    [Fact]
    public void Disable_mouse_reverses_the_same_flags()
    {
        Assert.Contains("1000l", TerminalEscapeSequences.DisableMouse);
        Assert.Contains("1006l", TerminalEscapeSequences.DisableMouse);
    }

    [Fact]
    public void Cursor_sequences_use_standard_codes()
    {
        Assert.Equal("\x1b[?25l", TerminalEscapeSequences.HideCursor);
        Assert.Equal("\x1b[?25h", TerminalEscapeSequences.ShowCursor);
    }
}
