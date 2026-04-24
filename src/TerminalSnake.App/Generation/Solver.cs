using System.Diagnostics;
using System.Text;
using TerminalSnake.Domain;
using TerminalSnake.Movement;

namespace TerminalSnake.Generation;

public static class Solver
{
    public const int DefaultStateLimit = 200_000;
    // BFS on a 40-wide board with a dozen long snakes can spend seconds
    // and gigabytes before it even picks up the first solution — the
    // generator makes 80 solvability checks per level, so a budget is
    // the difference between 'board ready in 50 ms' and 'five-second
    // stall on level 40' (issue #36 benchmark).
    public static readonly TimeSpan DefaultTimeBudget = TimeSpan.FromMilliseconds(100);

    public static IReadOnlyList<int>? TrySolve(
        Board board,
        int stateLimit = DefaultStateLimit,
        TimeSpan? timeBudget = null)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Snakes.Length == 0)
        {
            return Array.Empty<int>();
        }

        // Greedy covers every board where simply releasing the currently
        // unblocked snake (and iterating) works — the overwhelming
        // majority of procedurally generated boards. BFS stays available
        // for the rare case where a snake has to partially move so
        // another can pass through.
        if (TryGreedySolve(board) is { } greedy)
        {
            return greedy;
        }
        return BfsSolve(board, stateLimit, timeBudget ?? DefaultTimeBudget);
    }

    public static IReadOnlyList<int>? TryGreedySolve(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var current = board;
        var sequence = new List<int>(board.Snakes.Length);
        var guard = board.Snakes.Length + 1;
        while (current.Snakes.Length > 0 && guard-- > 0)
        {
            if (!TryReleaseOneSnake(ref current, sequence))
            {
                return null;
            }
        }
        return current.Snakes.Length == 0 ? sequence : null;
    }

    private static bool TryReleaseOneSnake(ref Board current, List<int> sequence)
    {
        for (var i = 0; i < current.Snakes.Length; i++)
        {
            var outcome = MoveEngine.Advance(current, i, captureFrames: false);
            if (!outcome.Exited)
            {
                continue;
            }
            sequence.Add(i);
            current = outcome.ResultingBoard;
            return true;
        }
        return false;
    }

    private static IReadOnlyList<int>? BfsSolve(Board board, int stateLimit, TimeSpan timeBudget)
    {
        var sw = Stopwatch.StartNew();
        var visited = new HashSet<string> { CanonicalState(board) };
        var queue = new Queue<(Board Board, int[] Sequence)>();
        queue.Enqueue((board, Array.Empty<int>()));

        while (queue.Count > 0)
        {
            if (sw.Elapsed > timeBudget || visited.Count >= stateLimit)
            {
                return null;
            }
            var (current, sequence) = queue.Dequeue();
            if (TryExpand(current, sequence, visited, queue, out var solution))
            {
                return solution;
            }
        }

        return null;
    }

    private static bool TryExpand(
        Board current,
        int[] sequence,
        HashSet<string> visited,
        Queue<(Board Board, int[] Sequence)> queue,
        out IReadOnlyList<int>? solution)
    {
        solution = null;
        for (var i = 0; i < current.Snakes.Length; i++)
        {
            var outcome = MoveEngine.Advance(current, i, captureFrames: false);
            if (outcome.Steps == 0)
            {
                continue;
            }
            var nextSequence = Append(sequence, i);
            if (outcome.Exited && outcome.ResultingBoard.Snakes.Length == 0)
            {
                solution = nextSequence;
                return true;
            }
            var nextState = CanonicalState(outcome.ResultingBoard);
            if (visited.Add(nextState))
            {
                queue.Enqueue((outcome.ResultingBoard, nextSequence));
            }
        }
        return false;
    }

    private static int[] Append(int[] sequence, int value)
    {
        var copy = new int[sequence.Length + 1];
        Array.Copy(sequence, copy, sequence.Length);
        copy[^1] = value;
        return copy;
    }

    private static string CanonicalState(Board board)
    {
        var builder = new StringBuilder();
        var ordered = board.Snakes.OrderBy(s => (int)s.Color);
        foreach (var snake in ordered)
        {
            AppendSnake(builder, snake);
        }
        return builder.ToString();
    }

    private static void AppendSnake(StringBuilder builder, Snake snake)
    {
        builder.Append((int)snake.Color).Append(':');
        foreach (var cell in snake.Segments)
        {
            builder.Append(cell.X).Append('.').Append(cell.Y).Append(',');
        }
        builder.Append('|');
    }
}
