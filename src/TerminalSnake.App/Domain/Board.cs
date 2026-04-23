using System.Collections.Immutable;

namespace TerminalSnake.Domain;

public sealed class Board
{
    private readonly ImmutableDictionary<Cell, int> _occupancy;

    public Board(int size, IEnumerable<Snake> snakes)
    {
        if (size < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Board size must be at least 3");
        }
        Size = size;
        Snakes = snakes.ToImmutableArray();
        _occupancy = BuildOccupancy(Snakes, size);
    }

    public int Size { get; }

    public ImmutableArray<Snake> Snakes { get; }

    public bool IsInside(Cell cell) =>
        cell.X >= 0 && cell.X < Size && cell.Y >= 0 && cell.Y < Size;

    public bool IsBorder(Cell cell) =>
        IsInside(cell) && (cell.X == 0 || cell.X == Size - 1 || cell.Y == 0 || cell.Y == Size - 1);

    public int? OccupyingSnake(Cell cell) =>
        _occupancy.TryGetValue(cell, out var index) ? index : null;

    public bool IsEmpty(Cell cell) => !_occupancy.ContainsKey(cell);

    public Board WithSnakes(IEnumerable<Snake> snakes) => new(Size, snakes);

    private static ImmutableDictionary<Cell, int> BuildOccupancy(
        ImmutableArray<Snake> snakes, int size)
    {
        var builder = ImmutableDictionary.CreateBuilder<Cell, int>();
        for (var snakeIndex = 0; snakeIndex < snakes.Length; snakeIndex++)
        {
            foreach (var cell in snakes[snakeIndex].Segments)
            {
                EnsureInBounds(cell, size, snakeIndex);
                EnsureNoOverlap(builder, cell, snakeIndex);
                builder.Add(cell, snakeIndex);
            }
        }
        return builder.ToImmutable();
    }

    private static void EnsureInBounds(Cell cell, int size, int snakeIndex)
    {
        if (cell.X < 0 || cell.X >= size || cell.Y < 0 || cell.Y >= size)
        {
            throw new ArgumentException(
                $"Snake {snakeIndex} segment {cell} is outside the {size}x{size} board", "snakes");
        }
    }

    private static void EnsureNoOverlap(
        ImmutableDictionary<Cell, int>.Builder builder, Cell cell, int snakeIndex)
    {
        if (builder.TryGetValue(cell, out var existing))
        {
            throw new ArgumentException(
                $"Snakes {existing} and {snakeIndex} overlap at {cell}", "snakes");
        }
    }
}
