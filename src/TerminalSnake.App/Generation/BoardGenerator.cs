using TerminalSnake.Domain;

namespace TerminalSnake.Generation;

public sealed class BoardGenerator
{
    public const int MaxSnakesPerBoard = 8;
    public const int MinBoardSize = 6;
    public const int MaxBoardSize = 16;
    public const int MaxSegmentLength = 8;
    public const int DefaultSolvableAttempts = 80;

    private const int SnakePlacementAttempts = 60;
    private const int MinSnakeLength = 3;

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

    public Board Generate(int levelIndex, int seed)
    {
        if (levelIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Level index must be ≥ 1");
        }

        var profile = DifficultyProfile.For(levelIndex);
        for (var attempt = 0; attempt < _solvableAttempts; attempt++)
        {
            var random = new Random(CombineSeed(seed, attempt));
            var board = TryBuildBoard(random, profile);
            if (board is not null && Solver.TrySolve(board) is not null)
            {
                return board;
            }
        }

        return FallbackBoard(profile);
    }

    private static int CombineSeed(int baseSeed, int attempt) =>
        unchecked(baseSeed * 397 ^ attempt);

    private static Board? TryBuildBoard(Random random, DifficultyProfile profile)
    {
        var occupied = new HashSet<Cell>();
        var snakes = new List<Snake>(profile.SnakeCount);
        for (var i = 0; i < profile.SnakeCount; i++)
        {
            var snake = TryPlaceSnake(random, profile, occupied, (SnakeColor)i);
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
            var targetLength = random.Next(MinSnakeLength, profile.MaxSnakeLength + 1);
            var segments = GrowSnake(random, profile.Size, occupied, head, second, targetLength);
            if (segments.Count >= MinSnakeLength)
            {
                return new Snake(segments, color);
            }
        }
        return null;
    }

    private static (Cell Head, Cell Second)? PickSeedPair(
        Random random, int size, HashSet<Cell> occupied)
    {
        var head = new Cell(random.Next(size), random.Next(size));
        if (occupied.Contains(head))
        {
            return null;
        }
        var direction = AllDirections[random.Next(AllDirections.Length)];
        var second = head + direction.Opposite().Delta();
        if (!InBounds(second, size) || occupied.Contains(second))
        {
            return null;
        }
        return (head, second);
    }

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
        if (!InBounds(candidate, size))
        {
            return false;
        }
        return !occupied.Contains(candidate) && !local.Contains(candidate);
    }

    private static bool InBounds(Cell cell, int size) =>
        cell.X >= 0 && cell.X < size && cell.Y >= 0 && cell.Y < size;

    private static Board FallbackBoard(DifficultyProfile profile)
    {
        // Minimal safe board: place parallel snakes one per row, each heading right.
        var snakes = new List<Snake>();
        var colors = Enum.GetValues<SnakeColor>();
        for (var i = 0; i < profile.SnakeCount && i < colors.Length; i++)
        {
            if (i >= profile.Size)
            {
                break;
            }
            var head = new Cell(1, i);
            var second = new Cell(0, i);
            snakes.Add(new Snake(new[] { head, second }, colors[i]));
        }
        return new Board(profile.Size, snakes);
    }

    internal sealed record DifficultyProfile(int Size, int SnakeCount, int MaxSnakeLength)
    {
        public static DifficultyProfile For(int levelIndex)
        {
            var size = Math.Clamp(MinBoardSize + levelIndex / 3, MinBoardSize, MaxBoardSize);
            var snakeCount = Math.Clamp(3 + levelIndex / 2, 3, MaxSnakesPerBoard);
            var maxLen = Math.Clamp(3 + levelIndex / 4, MinSnakeLength, MaxSegmentLength);
            return new DifficultyProfile(size, snakeCount, maxLen);
        }
    }
}
