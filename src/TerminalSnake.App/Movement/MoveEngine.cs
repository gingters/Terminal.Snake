using System.Collections.Immutable;
using TerminalSnake.Domain;

namespace TerminalSnake.Movement;

public static class MoveEngine
{
    public static MoveOutcome Advance(Board board, int snakeIndex, bool captureFrames = true)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (snakeIndex < 0 || snakeIndex >= board.Snakes.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(snakeIndex), snakeIndex, "Snake index out of range");
        }

        var snake = board.Snakes[snakeIndex];
        var segments = snake.Segments.ToList();
        var state = new AdvanceState(board, snake, segments, captureFrames);
        var (steps, blockedBy) = RunAdvance(state);

        return new MoveOutcome(
            ResultingBoard: ReplaceSnake(board, snakeIndex, segments, snake.Color),
            SnakeIndex: snakeIndex,
            Steps: steps,
            Exited: segments.Count == 0,
            BlockedBy: blockedBy,
            Frames: state.FinalFrames());
    }

    private static (int Steps, int? BlockedBy) RunAdvance(AdvanceState state)
    {
        var steps = 0;
        while (state.Segments.Count > 0)
        {
            var step = TryAdvanceOneStep(
                state.Segments, state.Delta, state.Board,
                state.TrackedOccupancy, state.CheapOccupancy);
            if (step.IsBlocked)
            {
                return (steps, step.BlockedBy);
            }
            steps++;
            state.RecordFrame();
        }
        return (steps, null);
    }

    // Holds per-Advance scratch buffers. Keeping them together lets
    // Advance itself stay under the project's complexity budget (the
    // old inline version was at 16; #36 benchmark).
    private sealed class AdvanceState
    {
        public Board Board { get; }
        public Cell Delta { get; }
        public List<Cell> Segments { get; }
        public Dictionary<Cell, int>? TrackedOccupancy { get; }
        public HashSet<Cell>? CheapOccupancy { get; }
        private readonly ImmutableArray<ImmutableArray<Cell>>.Builder? _frames;

        public AdvanceState(Board board, Snake snake, List<Cell> segments, bool captureFrames)
        {
            Board = board;
            Delta = snake.Direction.Delta();
            Segments = segments;
            if (captureFrames)
            {
                TrackedOccupancy = BuildTrackedOccupancyExcluding(board, IndexOf(board, snake));
                _frames = ImmutableArray.CreateBuilder<ImmutableArray<Cell>>();
            }
            else
            {
                CheapOccupancy = BuildCheapOccupancyExcluding(board, IndexOf(board, snake));
            }
        }

        public void RecordFrame() => _frames?.Add(Segments.ToImmutableArray());

        public ImmutableArray<ImmutableArray<Cell>> FinalFrames() =>
            _frames is null ? ImmutableArray<ImmutableArray<Cell>>.Empty : _frames.ToImmutable();

        private static int IndexOf(Board board, Snake snake)
        {
            for (var i = 0; i < board.Snakes.Length; i++)
            {
                if (ReferenceEquals(board.Snakes[i], snake))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    private static StepResult TryAdvanceOneStep(
        List<Cell> segments,
        Cell delta,
        Board board,
        Dictionary<Cell, int>? trackedOccupancy,
        HashSet<Cell>? cheapOccupancy)
    {
        var nextHead = segments[0] + delta;
        if (!board.IsInside(nextHead))
        {
            // Exit step: snake shrinks from the tail as the head moves off-board.
            segments.RemoveAt(segments.Count - 1);
            return StepResult.Moved;
        }
        if (trackedOccupancy is not null)
        {
            if (trackedOccupancy.TryGetValue(nextHead, out var blocker))
            {
                return StepResult.Blocked(blocker);
            }
        }
        else if (cheapOccupancy!.Contains(nextHead))
        {
            return StepResult.Blocked(SelfBlocker);
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

    private static Dictionary<Cell, int> BuildTrackedOccupancyExcluding(Board board, int excludedIndex)
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

    private static HashSet<Cell> BuildCheapOccupancyExcluding(Board board, int excludedIndex)
    {
        var set = new HashSet<Cell>();
        for (var i = 0; i < board.Snakes.Length; i++)
        {
            if (i == excludedIndex)
            {
                continue;
            }
            foreach (var cell in board.Snakes[i].Segments)
            {
                set.Add(cell);
            }
        }
        return set;
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
