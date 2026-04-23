using System.Collections.Immutable;

namespace TerminalSnake.Domain;

public sealed class Snake
{
    public Snake(IEnumerable<Cell> segments, SnakeColor color)
    {
        var snapshot = segments.ToImmutableArray();
        EnsureMinimumLength(snapshot);
        EnsureNoSelfIntersection(snapshot);
        EnsureConnectedPath(snapshot);
        Segments = snapshot;
        Color = color;
    }

    public ImmutableArray<Cell> Segments { get; }

    public SnakeColor Color { get; }

    public Cell Head => Segments[0];

    public Direction Direction => DirectionExtensions.FromDelta(Segments[1], Segments[0]);

    public Cell ExitCell(int boardSize)
    {
        if (boardSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be positive");
        }
        return Direction switch
        {
            Direction.Right => new Cell(boardSize - 1, Head.Y),
            Direction.Left => new Cell(0, Head.Y),
            Direction.Down => new Cell(Head.X, boardSize - 1),
            Direction.Up => new Cell(Head.X, 0),
            _ => throw new InvalidOperationException($"Unhandled direction: {Direction}"),
        };
    }

    public Snake WithSegments(IEnumerable<Cell> segments) => new(segments, Color);

    private static void EnsureMinimumLength(ImmutableArray<Cell> segments)
    {
        if (segments.Length < 2)
        {
            throw new ArgumentException("A snake needs at least 2 segments to determine its direction", nameof(segments));
        }
    }

    private static void EnsureNoSelfIntersection(ImmutableArray<Cell> segments)
    {
        var seen = new HashSet<Cell>(segments.Length);
        foreach (var cell in segments)
        {
            if (!seen.Add(cell))
            {
                throw new ArgumentException($"Snake self-intersects at {cell}", nameof(segments));
            }
        }
    }

    private static void EnsureConnectedPath(ImmutableArray<Cell> segments)
    {
        for (var i = 1; i < segments.Length; i++)
        {
            var previous = segments[i - 1];
            var current = segments[i];
            var dx = Math.Abs(previous.X - current.X);
            var dy = Math.Abs(previous.Y - current.Y);
            if (dx + dy != 1)
            {
                throw new ArgumentException(
                    $"Snake segments {previous} and {current} are not orthogonal neighbors", nameof(segments));
            }
        }
    }
}
