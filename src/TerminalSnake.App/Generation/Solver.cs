using System.Text;
using TerminalSnake.Domain;
using TerminalSnake.Movement;

namespace TerminalSnake.Generation;

public static class Solver
{
    public const int DefaultStateLimit = 200_000;

    public static IReadOnlyList<int>? TrySolve(Board board, int stateLimit = DefaultStateLimit)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.Snakes.Length == 0)
        {
            return Array.Empty<int>();
        }

        var visited = new HashSet<string> { CanonicalState(board) };
        var queue = new Queue<(Board Board, int[] Sequence)>();
        queue.Enqueue((board, Array.Empty<int>()));

        while (queue.Count > 0)
        {
            var (current, sequence) = queue.Dequeue();
            if (visited.Count >= stateLimit)
            {
                return null;
            }
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
            var outcome = MoveEngine.Advance(current, i);
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
