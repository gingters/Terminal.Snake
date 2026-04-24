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

        // Fast path first — most simple boards get released one snake at
        // a time without needing any partial-move trickery. For the
        // densely-packed boards issue #38 is about, the partial-move
        // solver picks up the slack: it handles "advance A three cells
        // so B's path opens up". BFS stays available behind the time
        // budget for niche cases.
        if (TryGreedySolve(board) is { } greedy)
        {
            return greedy;
        }
        if (TryPartialMoveSolve(board) is { } partial)
        {
            return partial;
        }
        return BfsSolve(board, stateLimit, timeBudget ?? DefaultTimeBudget);
    }

    /// <summary>
    /// Solves boards where simple "release one snake at a time" doesn't
    /// work but partial moves can untangle the knot. Each iteration
    /// picks the snake that advances the most cells (exits count as
    /// ∞) and commits that move; repeats until every snake is out or
    /// no snake can progress at all. Honours the supplied time budget
    /// so the generator can call it ~100 times without burning seconds
    /// on any single bad seed (#38).
    /// </summary>
    public static IReadOnlyList<int>? TryPartialMoveSolve(Board board, TimeSpan? timeBudget = null)
    {
        ArgumentNullException.ThrowIfNull(board);
        return RunPartialMoveSolve(board, timeBudget ?? DefaultPartialMoveBudget);
    }

    private static readonly TimeSpan DefaultPartialMoveBudget = TimeSpan.FromMilliseconds(100);

    private static IReadOnlyList<int>? RunPartialMoveSolve(Board start, TimeSpan budget)
    {
        var sw = Stopwatch.StartNew();
        var current = start;
        var sequence = new List<int>(start.Snakes.Length * 4);
        var guard = (current.Snakes.Length + 1) * (current.Size + 2);
        while (current.Snakes.Length > 0 && guard-- > 0)
        {
            if (sw.Elapsed > budget || !TryAdvanceAnySnake(ref current, sequence))
            {
                return null;
            }
        }
        return current.Snakes.Length == 0 ? sequence : null;
    }

    private static bool TryAdvanceAnySnake(ref Board current, List<int> sequence)
    {
        var bestIndex = -1;
        var bestSteps = 0;
        Board? bestBoard = null;
        for (var i = 0; i < current.Snakes.Length; i++)
        {
            var outcome = MoveEngine.Advance(current, i, captureFrames: false);
            if (outcome.Exited)
            {
                sequence.Add(i);
                current = outcome.ResultingBoard;
                return true;
            }
            if (outcome.Steps > bestSteps)
            {
                bestIndex = i;
                bestSteps = outcome.Steps;
                bestBoard = outcome.ResultingBoard;
            }
        }
        if (bestIndex < 0)
        {
            return false;
        }
        sequence.Add(bestIndex);
        current = bestBoard!;
        return true;
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
