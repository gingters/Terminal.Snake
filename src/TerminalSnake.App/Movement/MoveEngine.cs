using System.Collections.Immutable;
using TerminalSnake.Domain;

namespace TerminalSnake.Movement;

public static class MoveEngine
{
    public static MoveOutcome Advance(Board board, int snakeIndex)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (snakeIndex < 0 || snakeIndex >= board.Snakes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(snakeIndex), snakeIndex, "Snake index out of range");
        }

        var snake = board.Snakes[snakeIndex];
        var delta = snake.Direction.Delta();
        var segments = snake.Segments.ToList();
        var otherOccupancy = BuildOccupancyExcluding(board, snakeIndex);
        var frames = ImmutableArray.CreateBuilder<ImmutableArray<Cell>>();
        int? blockedBy = null;

        while (segments.Count > 0)
        {
            var step = TryAdvanceOneStep(segments, delta, board, otherOccupancy);
            if (step.IsBlocked)
            {
                blockedBy = step.BlockedBy;
                break;
            }
            frames.Add(segments.ToImmutableArray());
        }

        return new MoveOutcome(
            ResultingBoard: ReplaceSnake(board, snakeIndex, segments, snake.Color),
            SnakeIndex: snakeIndex,
            Steps: frames.Count,
            Exited: segments.Count == 0,
            BlockedBy: blockedBy,
            Frames: frames.ToImmutable());
    }

    private static StepResult TryAdvanceOneStep(
        List<Cell> segments,
        Cell delta,
        Board board,
        IReadOnlyDictionary<Cell, int> otherOccupancy)
    {
        var nextHead = segments[0] + delta;
        if (!board.IsInside(nextHead))
        {
            // Exit step: snake shrinks from the tail as the head moves off-board.
            segments.RemoveAt(segments.Count - 1);
            return StepResult.Moved;
        }
        if (otherOccupancy.TryGetValue(nextHead, out var blocker))
        {
            return StepResult.Blocked(blocker);
        }
        if (IsSelfCollision(segments, nextHead))
        {
            return StepResult.Blocked(SelfBlocker);
        }
        segments.Insert(0, nextHead);
        segments.RemoveAt(segments.Count - 1);
        return StepResult.Moved;
    }

    private static bool IsSelfCollision(List<Cell> segments, Cell nextHead)
    {
        // The tail cell is about to be vacated; every other segment remains occupied.
        for (var i = 0; i < segments.Count - 1; i++)
        {
            if (segments[i] == nextHead)
            {
                return true;
            }
        }
        return false;
    }

    private static IReadOnlyDictionary<Cell, int> BuildOccupancyExcluding(Board board, int excludedIndex)
    {
        var map = new Dictionary<Cell, int>();
        for (var i = 0; i < board.Snakes.Length; i++)
        {
            if (i == excludedIndex)
            {
                continue;
            }
            foreach (var cell in board.Snakes[i].Segments)
            {
                map[cell] = i;
            }
        }
        return map;
    }

    private static Board ReplaceSnake(Board board, int snakeIndex, List<Cell> segments, SnakeColor color)
    {
        var builder = ImmutableArray.CreateBuilder<Snake>(board.Snakes.Length);
        for (var i = 0; i < board.Snakes.Length; i++)
        {
            if (i == snakeIndex)
            {
                if (segments.Count == 0)
                {
                    continue;
                }
                builder.Add(new Snake(segments, color));
            }
            else
            {
                builder.Add(board.Snakes[i]);
            }
        }
        return board.WithSnakes(builder.ToImmutable());
    }

    internal const int SelfBlocker = -1;

    private readonly record struct StepResult(bool IsBlocked, int BlockedBy)
    {
        public static StepResult Moved => new(false, 0);
        public static StepResult Blocked(int by) => new(true, by);
    }
}
