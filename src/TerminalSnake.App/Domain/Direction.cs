namespace TerminalSnake.Domain;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

public static class DirectionExtensions
{
    public static Cell Delta(this Direction direction) => direction switch
    {
        Direction.Up => new Cell(0, -1),
        Direction.Down => new Cell(0, 1),
        Direction.Left => new Cell(-1, 0),
        Direction.Right => new Cell(1, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction"),
    };

    public static Direction Opposite(this Direction direction) => direction switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction"),
    };

    public static Direction FromDelta(Cell from, Cell to)
    {
        var delta = to - from;
        return (delta.X, delta.Y) switch
        {
            (0, -1) => Direction.Up,
            (0, 1) => Direction.Down,
            (-1, 0) => Direction.Left,
            (1, 0) => Direction.Right,
            _ => throw new ArgumentException(
                $"Cells {from} and {to} are not orthogonal neighbors", nameof(to)),
        };
    }
}
