using TerminalSnake.Domain;

namespace TerminalSnake.Generation;

/// <summary>
/// Builds guaranteed-solvable boards by placing snakes in *reverse*
/// release order: snake N goes down first with no constraints (nothing
/// exits after it), then snake N-1 with its head-to-border ray forced
/// clear of snake N's body, and so on down to snake 1 which has to
/// thread a path past every other snake that's still on the board at
/// its turn. Constructive by construction — no accept/reject loop
/// over random placements, so the generator can reliably produce
/// dense boards that the random-walk builder used to throw away.
/// Issue #38.
/// </summary>
internal static class ConstructiveBoardBuilder
{
    private const int PlacementAttemptsPerSnake = 80;
    private const int HeadAttemptsPerPlacement = 40;
    private const int ColorCount = 8;

    private static readonly Direction[] AllDirections =
    {
        Direction.Up,
        Direction.Down,
        Direction.Left,
        Direction.Right,
    };

    public static Board? TryBuild(Random random, int boardSize, int snakeCount, int minLen, int maxLen)
    {
        var occupied = new HashSet<Cell>();
        var snakes = new List<Snake>(snakeCount);
        for (var reverseIndex = snakeCount - 1; reverseIndex >= 0; reverseIndex--)
        {
            var color = (SnakeColor)(reverseIndex % ColorCount);
            var snake = TryPlace(random, boardSize, occupied, minLen, maxLen, color);
            if (snake is null)
            {
                return null;
            }
            foreach (var cell in snake.Segments)
            {
                occupied.Add(cell);
            }
            snakes.Add(snake);
        }
        snakes.Reverse();
        return new Board(boardSize, snakes);
    }

    private static Snake? TryPlace(
        Random random, int size, HashSet<Cell> occupied, int minLen, int maxLen, SnakeColor color)
    {
        for (var attempt = 0; attempt < PlacementAttemptsPerSnake; attempt++)
        {
            var direction = AllDirections[random.Next(AllDirections.Length)];
            var head = PickHeadWithClearForwardRay(random, size, direction, occupied);
            if (head is null)
            {
                continue;
            }
            var second = head.Value + direction.Opposite().Delta();
            if (!IsInnerCell(second, size) || occupied.Contains(second))
            {
                continue;
            }
            var ownRay = CollectForwardRay(head.Value, direction, size);
            var targetLen = random.Next(minLen, maxLen + 1);
            var body = GrowBody(random, size, occupied, ownRay, head.Value, second, targetLen);
            if (body.Count >= minLen)
            {
                return new Snake(body, color);
            }
        }
        return null;
    }

    private static Cell? PickHeadWithClearForwardRay(
        Random random, int size, Direction direction, HashSet<Cell> occupied)
    {
        var innerMax = size - 2;
        if (innerMax < 1)
        {
            return null;
        }
        for (var attempt = 0; attempt < HeadAttemptsPerPlacement; attempt++)
        {
            var head = new Cell(1 + random.Next(innerMax), 1 + random.Next(innerMax));
            if (occupied.Contains(head))
            {
                continue;
            }
            if (ForwardRayIsClear(head, direction, size, occupied))
            {
                return head;
            }
        }
        return null;
    }

    private static bool ForwardRayIsClear(Cell head, Direction direction, int size, HashSet<Cell> occupied)
    {
        var delta = direction.Delta();
        var cursor = head + delta;
        while (cursor.X >= 0 && cursor.X < size && cursor.Y >= 0 && cursor.Y < size)
        {
            if (occupied.Contains(cursor))
            {
                return false;
            }
            cursor += delta;
        }
        return true;
    }

    private static HashSet<Cell> CollectForwardRay(Cell head, Direction direction, int size)
    {
        var ray = new HashSet<Cell>();
        var delta = direction.Delta();
        var cursor = head + delta;
        while (cursor.X >= 0 && cursor.X < size && cursor.Y >= 0 && cursor.Y < size)
        {
            ray.Add(cursor);
            cursor += delta;
        }
        return ray;
    }

    private static List<Cell> GrowBody(
        Random random,
        int size,
        HashSet<Cell> occupied,
        HashSet<Cell> ownRay,
        Cell head,
        Cell second,
        int targetLen)
    {
        var segments = new List<Cell> { head, second };
        var local = new HashSet<Cell> { head, second };
        while (segments.Count < targetLen)
        {
            var next = PickNextSegment(random, size, occupied, local, ownRay, segments[^1], segments[^2]);
            if (next is null)
            {
                break;
            }
            segments.Add(next.Value);
            local.Add(next.Value);
        }
        return segments;
    }

    private static Cell? PickNextSegment(
        Random random,
        int size,
        HashSet<Cell> occupied,
        HashSet<Cell> local,
        HashSet<Cell> ownRay,
        Cell tail,
        Cell prev)
    {
        var candidates = new List<Cell>(AllDirections.Length);
        foreach (var direction in AllDirections)
        {
            var candidate = tail + direction.Delta();
            if (IsEligibleGrowthStep(candidate, prev, size, occupied, local, ownRay))
            {
                candidates.Add(candidate);
            }
        }
        return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
    }

    private static bool IsEligibleGrowthStep(
        Cell candidate, Cell prev, int size,
        HashSet<Cell> occupied, HashSet<Cell> local, HashSet<Cell> ownRay)
    {
        if (candidate == prev)
        {
            return false;
        }
        if (!IsInnerCell(candidate, size))
        {
            return false;
        }
        return !occupied.Contains(candidate)
            && !local.Contains(candidate)
            && !ownRay.Contains(candidate);
    }

    private static bool IsInnerCell(Cell cell, int size) =>
        cell.X >= 1 && cell.X <= size - 2 && cell.Y >= 1 && cell.Y <= size - 2;
}
