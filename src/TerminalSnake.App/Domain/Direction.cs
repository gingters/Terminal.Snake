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
    private static readonly Cell[] DeltaByDirection =
    {
        new(0, -1), // Up
        new(0, 1),  // Down
        new(-1, 0), // Left
        new(1, 0),  // Right
    };

    private static readonly Direction[] OppositeOf =
    {
        Direction.Down,  // Up
        Direction.Up,    // Down
        Direction.Right, // Left
        Direction.Left,  // Right
    };

    private static readonly Dictionary<Cell, Direction> DirectionByDelta = new()
    {
        [new Cell(0, -1)] = Direction.Up,
        [new Cell(0, 1)] = Direction.Down,
        [new Cell(-1, 0)] = Direction.Left,
        [new Cell(1, 0)] = Direction.Right,
    };

    public static Cell Delta(this Direction direction)
    {
        EnsureValid(direction);
        return DeltaByDirection[(int)direction];
    }

    public static Direction Opposite(this Direction direction)
    {
        EnsureValid(direction);
        return OppositeOf[(int)direction];
    }

    public static Direction FromDelta(Cell from, Cell to)
    {
        var delta = to - from;
        if (DirectionByDelta.TryGetValue(delta, out var direction))
        {
            return direction;
        }
        throw new ArgumentException(
            $"Cells {from} and {to} are not orthogonal neighbors", nameof(to));
    }

    private static void EnsureValid(Direction direction)
    {
        var value = (int)direction;
        if (value < 0 || value >= DeltaByDirection.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction");
        }
    }
}
