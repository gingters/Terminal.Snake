using System.Text;
using TerminalSnake.Domain;

namespace TerminalSnake.Rendering;

public sealed class FrameBuffer
{
    private readonly Glyph[,] _glyphs;

    public FrameBuffer(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width), $"Frame buffer dimensions must be positive (got {width}x{height})");
        }
        Width = width;
        Height = height;
        _glyphs = new Glyph[width, height];
        Clear();
    }

    public int Width { get; }

    public int Height { get; }

    public Glyph this[int x, int y]
    {
        get
        {
            EnsureInside(x, y);
            return _glyphs[x, y];
        }
        set
        {
            EnsureInside(x, y);
            _glyphs[x, y] = value;
        }
    }

    public void Clear(char fill = ' ', SnakeColor? foreground = null, SnakeColor? background = null)
    {
        var glyph = new Glyph(fill, foreground, background);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                _glyphs[x, y] = glyph;
            }
        }
    }

    public void Set(int x, int y, char ch, SnakeColor? foreground = null, SnakeColor? background = null)
    {
        EnsureInside(x, y);
        _glyphs[x, y] = new Glyph(ch, foreground, background);
    }

    public void DrawHorizontalLine(int x, int y, int length, char ch)
    {
        for (var i = 0; i < length; i++)
        {
            Set(x + i, y, ch);
        }
    }

    public void DrawVerticalLine(int x, int y, int length, char ch)
    {
        for (var i = 0; i < length; i++)
        {
            Set(x, y + i, ch);
        }
    }

    public string Snapshot()
    {
        var builder = new StringBuilder((Width + 1) * Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                builder.Append(_glyphs[x, y].Char);
            }
            if (y < Height - 1)
            {
                builder.Append('\n');
            }
        }
        return builder.ToString();
    }

    private void EnsureInside(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(
                $"({x},{y})", $"Coordinates must be inside the {Width}x{Height} frame buffer");
        }
    }

    public readonly record struct Glyph(char Char, SnakeColor? Foreground, SnakeColor? Background);
}
