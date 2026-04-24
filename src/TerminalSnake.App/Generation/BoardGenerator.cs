using TerminalSnake.Domain;

namespace TerminalSnake.Generation;

public sealed class BoardGenerator
{
    public const int MinBoardSize = 6;
    // Default cap used when no explicit terminal size is supplied — matches
    // the old placement grid so tutorial-tight boards keep their existing
    // density. Runtime callers pass a larger cap derived from the terminal
    // (up to AbsoluteMaxBoardSize) so the play area scales with the screen.
    public const int MaxBoardSize = 16;
    public const int AbsoluteMaxBoardSize = 64;
    public const int MaxSegmentLength = 120;
    public const int DefaultSolvableAttempts = 80;

    // Hard cap on how many snakes a board can carry. There are 8 distinct
    // colours in SnakeColor and we cycle past that for very large boards,
    // so snakes on big boards may repeat a colour — the player still reads
    // them apart by their position on the grid.
    public const int MaxSnakesPerBoard = 32;
    private const int ColorCount = 8;

    // Long-snake profiles make random-walk placement harder; allow more
    // placement retries before failing a whole board attempt.
    private const int SnakePlacementAttempts = 120;
    private const int AbsoluteMinSnakeLength = 3;

    private static readonly Direction[] AllDirections =
    {
        Direction.Up,
        Direction.Down,
        Direction.Left,
        Direction.Right,
    };

    private readonly int _solvableAttempts;

    public BoardGenerator()
        : this(DefaultSolvableAttempts)
    {
    }

    public BoardGenerator(int solvableAttempts)
    {
        if (solvableAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(solvableAttempts), solvableAttempts, "Solvable attempts must be non-negative");
        }
        _solvableAttempts = solvableAttempts;
    }

    public Board Generate(int levelIndex, int seed, int maxBoardSide = MaxBoardSize)
    {
        if (levelIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Level index must be ≥ 1");
        }

        var boardSize = DetermineBoardSize(levelIndex, maxBoardSide);
        var profile = DifficultyProfile.For(levelIndex, boardSize);
        var requireChallenge = levelIndex >= 2;
        for (var attempt = 0; attempt < _solvableAttempts; attempt++)
        {
            var random = new Random(CombineSeed(seed, attempt));
            var candidate = TryBuildAcceptableBoard(random, profile, requireChallenge);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return FallbackBoard(profile);
    }

    // Board size grows by 1 per level past level 1, clamped to what the
    // terminal can render (issue #36). Snakes are placed directly on this
    // grid — scaled-up boards actually carry scaled-up snake counts and
    // lengths now, instead of centring a small layout on a huge blank
    // field like the first #36 iteration did.
    private static int DetermineBoardSize(int levelIndex, int maxBoardSide)
    {
        var desired = MinBoardSize + Math.Max(0, levelIndex - 1);
        var cap = Math.Max(MinBoardSize, Math.Min(AbsoluteMaxBoardSize, maxBoardSide));
        return Math.Clamp(desired, MinBoardSize, cap);
    }

    private static Board? TryBuildAcceptableBoard(Random random, DifficultyProfile profile, bool requireChallenge)
    {
        var board = TryBuildBoard(random, profile);
        if (board is null)
        {
            return null;
        }
        if (requireChallenge && !HasInitiallyBlockedSnake(board))
        {
            return null;
        }
        // Greedy-only solvability check — BFS is prohibitively expensive
        // on the 40-wide boards level 100+ produces (6 GB / 4 s in the
        // pre-change benchmark). Boards that only succeed via partial-
        // move trickery are rare; rejecting them in generation is fine
        // because the caller just retries with the next seed.
        return Solver.TryGreedySolve(board) is null ? null : board;
    }

    /// <summary>
    /// True if at least one snake cannot walk straight out of the field on
    /// turn one because another snake's body sits in its forward path.
    /// Rejecting boards where this is false avoids the "press Enter three
    /// times, level done" flow reported in issue #14.
    /// </summary>
    private static bool HasInitiallyBlockedSnake(Board board)
    {
        foreach (var snake in board.Snakes)
        {
            if (IsForwardPathBlocked(board, snake))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsForwardPathBlocked(Board board, Snake snake)
    {
        var ownHead = snake.Head;
        var delta = snake.Direction.Delta();
        var cursor = ownHead + delta;
        while (board.IsInside(cursor))
        {
            var occupant = board.OccupyingSnake(cursor);
            if (occupant is int index && !ReferenceEquals(board.Snakes[index], snake))
            {
                return true;
            }
            cursor += delta;
        }
        return false;
    }

    private static int CombineSeed(int baseSeed, int attempt) =>
        unchecked(baseSeed * 397 ^ attempt);

    private static Board? TryBuildBoard(Random random, DifficultyProfile profile)
    {
        var occupied = new HashSet<Cell>();
        var snakes = new List<Snake>(profile.SnakeCount);
        for (var i = 0; i < profile.SnakeCount; i++)
        {
            var snake = TryPlaceSnake(random, profile, occupied, (SnakeColor)(i % ColorCount));
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
        return new Board(profile.Size, snakes);
    }

    private static Snake? TryPlaceSnake(
        Random random, DifficultyProfile profile, HashSet<Cell> occupied, SnakeColor color)
    {
        for (var attempt = 0; attempt < SnakePlacementAttempts; attempt++)
        {
            var seed = PickSeedPair(random, profile.Size, occupied);
            if (seed is null)
            {
                continue;
            }
            var (head, second) = seed.Value;
            var targetLength = random.Next(profile.MinSnakeLength, profile.MaxSnakeLength + 1);
            var segments = GrowSnake(random, profile.Size, occupied, head, second, targetLength);
            // Reject snakes that couldn't reach the profile's minimum length —
            // accepting a 3-segment snake when the target was 10 would quietly
            // regress the "snakes must be longer" ask behind issue #13.
            if (segments.Count >= profile.MinSnakeLength)
            {
                return new Snake(segments, color);
            }
        }
        return null;
    }

    private static (Cell Head, Cell Second)? PickSeedPair(
        Random random, int size, HashSet<Cell> occupied)
    {
        // Head is biased toward the snake's exit border so the forward
        // ray it needs to traverse to leave the board is short — without
        // that bias, on a 40-wide board every snake's head-to-border ray
        // has a ~99.9 % chance of crossing another snake and the
        // greedy solver fails on every seed (issue #36).
        var innerMax = size - 2;
        if (innerMax < 1)
        {
            return null;
        }
        var direction = AllDirections[random.Next(AllDirections.Length)];
        var head = PickHeadNearExitBorder(random, size, direction);
        if (occupied.Contains(head))
        {
            return null;
        }
        var second = head + direction.Opposite().Delta();
        if (!IsInnerCell(second, size) || occupied.Contains(second))
        {
            return null;
        }
        return (head, second);
    }

    private static Cell PickHeadNearExitBorder(Random random, int size, Direction direction)
    {
        // Leave the head within the last HeadExitZone cells of the inner
        // region in the snake's exit direction; the body grows backward
        // through the rest of the board via the random walk.
        const int HeadExitZone = 4;
        var innerMax = size - 2;
        var band = Math.Min(HeadExitZone, innerMax);
        var front = 1 + innerMax - band + random.Next(band);
        var back = 1 + random.Next(innerMax);
        return direction switch
        {
            Direction.Right => new Cell(front, back),
            Direction.Left => new Cell(size - 1 - front, back),
            Direction.Down => new Cell(back, front),
            Direction.Up => new Cell(back, size - 1 - front),
            _ => new Cell(back, back),
        };
    }

    private static bool IsInnerCell(Cell cell, int size) =>
        cell.X >= 1 && cell.X <= size - 2 && cell.Y >= 1 && cell.Y <= size - 2;

    private static List<Cell> GrowSnake(
        Random random, int size, HashSet<Cell> occupied, Cell head, Cell second, int targetLength)
    {
        var segments = new List<Cell> { head, second };
        var local = new HashSet<Cell> { head, second };
        while (segments.Count < targetLength)
        {
            var nextCell = PickNextSegment(random, size, occupied, local, segments[^1], segments[^2]);
            if (nextCell is null)
            {
                break;
            }
            segments.Add(nextCell.Value);
            local.Add(nextCell.Value);
        }
        return segments;
    }

    private static Cell? PickNextSegment(
        Random random, int size, HashSet<Cell> occupied, HashSet<Cell> local, Cell tail, Cell prev)
    {
        var candidates = CollectGrowthCandidates(size, occupied, local, tail, prev);
        return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
    }

    private static List<Cell> CollectGrowthCandidates(
        int size, HashSet<Cell> occupied, HashSet<Cell> local, Cell tail, Cell prev)
    {
        var candidates = new List<Cell>(AllDirections.Length);
        foreach (var direction in AllDirections)
        {
            var candidate = tail + direction.Delta();
            if (IsValidGrowthStep(candidate, prev, size, occupied, local))
            {
                candidates.Add(candidate);
            }
        }
        return candidates;
    }

    private static bool IsValidGrowthStep(
        Cell candidate, Cell prev, int size, HashSet<Cell> occupied, HashSet<Cell> local)
    {
        if (candidate == prev)
        {
            return false;
        }
        if (!IsInnerCell(candidate, size))
        {
            return false;
        }
        return !occupied.Contains(candidate) && !local.Contains(candidate);
    }

    private static Board FallbackBoard(DifficultyProfile profile)
    {
        // Minimal safe board: place parallel snakes one per row inside the
        // padded inner region so every fallback snake still satisfies the
        // "≥ 1 cell from the border" rule (#36).
        var snakes = new List<Snake>();
        for (var i = 0; i < profile.SnakeCount && i + 1 < profile.Size - 1; i++)
        {
            var color = (SnakeColor)(i % ColorCount);
            var head = new Cell(2, i + 1);
            var second = new Cell(1, i + 1);
            snakes.Add(new Snake(new[] { head, second }, color));
        }
        return new Board(profile.Size, snakes);
    }

    internal sealed record DifficultyProfile(int Size, int SnakeCount, int MinSnakeLength, int MaxSnakeLength)
    {
        // Snake counts and lengths scale with the ACTUAL board size now
        // (issue #36 follow-up): on a 40x40 grid at level 999 we want
        // a double-digit snake count with snakes long enough to wrap
        // most of the way around the field, not 8 tutorial-sized snakes
        // clustered in the middle.
        public static DifficultyProfile For(int levelIndex, int boardSize)
        {
            var inner = Math.Max(1, boardSize - 2);
            var snakeCount = ComputeSnakeCount(levelIndex, inner);
            var minLen = Math.Clamp(
                AbsoluteMinSnakeLength + levelIndex / 4,
                AbsoluteMinSnakeLength,
                Math.Max(AbsoluteMinSnakeLength, inner));
            var legacyMax = AbsoluteMinSnakeLength + levelIndex;
            // Cap snake length at ~inner so the random-walk placement
            // doesn't get stuck chasing its tail: total coverage stays
            // around 30 % (snakeCount × maxLen ÷ inner²), which the
            // generator can reliably satisfy.
            var sizeCap = Math.Max(minLen + 1, inner);
            var maxLen = Math.Clamp(
                Math.Min(legacyMax, sizeCap),
                AbsoluteMinSnakeLength + 1,
                MaxSegmentLength);
            return new DifficultyProfile(boardSize, snakeCount, minLen, maxLen);
        }

        private static int ComputeSnakeCount(int levelIndex, int inner)
        {
            // Legacy tutorial pacing for small boards (8 snakes at level
            // 15 on a 16-grid). The size-driven cap kicks in on bigger
            // boards — keep roughly half the inner side's worth of snakes
            // so the density stays close to the old 16-grid layout and
            // the HasInitiallyBlockedSnake challenge check is actually
            // satisfiable (at low density every head points straight out
            // at empty space and the generator rejects every seed).
            var levelDriven = 3 + levelIndex / 3;
            var sizeFloor = inner / 3;
            var sizeCap = Math.Max(8, inner / 3);
            return Math.Clamp(
                Math.Max(levelDriven, sizeFloor),
                3,
                Math.Min(MaxSnakesPerBoard, sizeCap));
        }
    }
}
