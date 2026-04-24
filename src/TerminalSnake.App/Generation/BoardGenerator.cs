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
    public const int DefaultSolvableAttempts = 120;

    // Hard cap on how many snakes a board can carry. There are 8 distinct
    // colours in SnakeColor and we cycle past that for very large boards,
    // so snakes on big boards may repeat a colour — the player still reads
    // them apart by their position on the grid.
    public const int MaxSnakesPerBoard = 48;
    private const int ColorCount = 8;

    private const int AbsoluteMinSnakeLength = 3;

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
        var cap = Math.Max(MinBoardSize, Math.Min(AbsoluteMaxBoardSize, maxBoardSide));
        if (levelIndex >= FullBoardLevel)
        {
            return cap;
        }
        // Tutorial ramp: level 1 → MinBoardSize, level FullBoardLevel →
        // full terminal. Linear so the grid visibly grows every level
        // in the first few puzzles instead of staying cramped in the
        // middle of a huge terminal (#36 image review: level 8 was
        // half-empty on a 50-wide screen).
        var tutorialRange = cap - MinBoardSize;
        var scaled = MinBoardSize + (tutorialRange * (levelIndex - 1)) / (FullBoardLevel - 1);
        return Math.Clamp(scaled, MinBoardSize, cap);
    }

    // Level at which the board is expected to already fill the entire
    // terminal. Beyond this point the difficulty ramps via snake count
    // and length instead of size.
    private const int FullBoardLevel = 5;

    private static Board? TryBuildAcceptableBoard(Random random, DifficultyProfile profile, bool requireChallenge)
    {
        // Constructive placement — snakes go down in reverse release
        // order with each snake's forward ray forced clear of the
        // already-placed bodies, so solvability is guaranteed by
        // construction (#38). If the per-snake attempt budget runs
        // out, return null and let the outer retry loop pick a new
        // seed; there's no accept/reject solvability check to run
        // afterwards because construction *is* the check.
        var board = ConstructiveBoardBuilder.TryBuild(
            random, profile.Size, profile.SnakeCount, profile.MinSnakeLength, profile.MaxSnakeLength);
        if (board is null)
        {
            return null;
        }
        if (requireChallenge && !HasInitiallyBlockedSnake(board))
        {
            return null;
        }
        return board;
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

    private static int CombineSeed(int baseSeed, int attempt)
    {
        // Splitmix-style deterministic mixer. The old `baseSeed * 397
        // ^ attempt` occasionally produced correlated bad runs where
        // 500 consecutive attempts all failed (#36 image review: level
        // 512 on a 60-wide terminal kept falling back). Need a mixer
        // that (a) is stable across .NET sessions — unlike HashCode.Combine
        // — and (b) actually spreads bits across the output.
        unchecked
        {
            var x = (uint)baseSeed * 2654435761U;
            x ^= (uint)attempt * 40503U;
            x ^= x >> 16;
            x *= 2246822519U;
            x ^= x >> 13;
            x *= 3266489917U;
            x ^= x >> 16;
            return (int)x;
        }
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
            // Constructive placement can carry real density — issue #38.
            // Target ~60 % coverage: each snake takes a solid chunk of
            // the inner side. minLen keeps snakes from ending up stubby,
            // maxLen uses a per-snake budget off the 60 % target.
            var minLen = Math.Clamp(
                AbsoluteMinSnakeLength + levelIndex / 2,
                Math.Max(AbsoluteMinSnakeLength, inner / 3),
                Math.Max(AbsoluteMinSnakeLength, (inner * 2) / 3));
            var levelDriven = AbsoluteMinSnakeLength + levelIndex * 3;
            var sizeFloor = (inner * 3) / 2;
            var rawMax = Math.Max(levelDriven, sizeFloor);
            var perSnakeBudget = Math.Max(minLen + 1, (inner * inner * 3) / (5 * Math.Max(1, snakeCount)));
            var maxLen = Math.Clamp(
                Math.Min(rawMax, perSnakeBudget),
                AbsoluteMinSnakeLength + 1,
                MaxSegmentLength);
            return new DifficultyProfile(boardSize, snakeCount, minLen, maxLen);
        }

        private static int ComputeSnakeCount(int levelIndex, int inner)
        {
            // Constructive placement supports denser boards than
            // random-walk + accept/reject ever could. Push count to
            // ~⅔ × inner for mid+ levels — on a 50-wide board that's
            // ~32 snakes of ~40 cells, pressing past 60 % coverage.
            var levelDriven = 3 + levelIndex / 3;
            var sizeFloor = (inner * 2) / 3;
            var sizeCap = Math.Max(8, sizeFloor);
            return Math.Clamp(
                Math.Max(levelDriven, sizeFloor),
                3,
                Math.Min(MaxSnakesPerBoard, sizeCap));
        }
    }
}
