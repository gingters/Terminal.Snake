using Spectre.Console;
using TerminalSnake.Domain;
using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Rendering;

public sealed class BoardViewTests
{
    [Fact]
    public void Constructor_rejects_null_buffer()
    {
        Assert.Throws<ArgumentNullException>(() => new BoardView(null!));
    }

    [Fact]
    public void Update_rejects_null_buffer()
    {
        var view = new BoardView(new FrameBuffer(3, 3));
        Assert.Throws<ArgumentNullException>(() => view.Update(null!));
    }

    [Fact]
    public void Update_replaces_the_current_buffer()
    {
        var original = new FrameBuffer(3, 3);
        var replacement = new FrameBuffer(5, 5);
        var view = new BoardView(original);
        view.Update(replacement);
        Assert.Same(replacement, view.Buffer);
    }

    [Fact]
    public void Rendering_through_spectre_produces_ansi_output_with_buffer_chars()
    {
        var buffer = new FrameBuffer(4, 2);
        buffer.Set(0, 0, 'A', SnakeColor.Red);
        buffer.Set(1, 0, 'B');
        buffer.Set(0, 1, 'C');
        var view = new BoardView(buffer);

        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(new StringWriter()),
        });
        console.Write(view);
        var output = console.Profile.Out.Writer.ToString();

        Assert.NotNull(output);
        Assert.Contains("A", output);
        Assert.Contains("B", output);
        Assert.Contains("C", output);
    }

    [Fact]
    public void Rendering_emits_line_breaks_only_between_rows_not_after_the_last()
    {
        // A trailing line break on the final row would push the cursor past
        // the visible area and scroll the terminal buffer on each refresh —
        // that was the visible "jump" on redraw reported in issue #2.
        var buffer = new FrameBuffer(3, 4);
        var view = new BoardView(buffer);
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer),
        });
        console.Write(view);
        var output = writer.ToString();

        // Height 4 rows → 3 line breaks between them, none after the last.
        var newlines = output.Count(c => c == '\n');
        Assert.Equal(buffer.Height - 1, newlines);
    }
}
