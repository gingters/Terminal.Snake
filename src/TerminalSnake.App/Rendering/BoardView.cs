using Spectre.Console.Rendering;

namespace TerminalSnake.Rendering;

public sealed class BoardView : Renderable
{
    private FrameBuffer _buffer;

    public BoardView(FrameBuffer initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        _buffer = initial;
    }

    public void Update(FrameBuffer next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _buffer = next;
    }

    public FrameBuffer Buffer => _buffer;

    protected override Measurement Measure(RenderOptions options, int maxWidth) =>
        new(_buffer.Width, _buffer.Width);

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        for (var y = 0; y < _buffer.Height; y++)
        {
            foreach (var segment in RenderRow(y))
            {
                yield return segment;
            }
            yield return Segment.LineBreak;
        }
    }

    private IEnumerable<Segment> RenderRow(int y)
    {
        for (var x = 0; x < _buffer.Width; x++)
        {
            var glyph = _buffer[x, y];
            var style = Theme.BuildStyle(glyph.Foreground, glyph.Background);
            yield return new Segment(glyph.Char.ToString(), style);
        }
    }
}
