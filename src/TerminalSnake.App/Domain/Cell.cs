namespace TerminalSnake.Domain;

public readonly record struct Cell(int X, int Y)
{
    public static Cell operator +(Cell left, Cell right) => new(left.X + right.X, left.Y + right.Y);

    public static Cell operator -(Cell left, Cell right) => new(left.X - right.X, left.Y - right.Y);

    public override string ToString() => $"({X},{Y})";
}
