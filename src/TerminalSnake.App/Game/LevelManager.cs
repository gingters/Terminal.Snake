using TerminalSnake.Domain;
using TerminalSnake.Generation;

namespace TerminalSnake.Game;

public sealed class LevelManager
{
    private readonly BoardGenerator _generator;

    public LevelManager()
        : this(new BoardGenerator())
    {
    }

    public LevelManager(BoardGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        _generator = generator;
    }

    public Board LoadLevel(int levelIndex, int maxBoardSide = BoardGenerator.MaxBoardSize)
    {
        if (levelIndex < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(levelIndex), levelIndex, "Level index must be >= 1");
        }
        if (levelIndex <= FixedLevels.Count)
        {
            return FixedLevels.Get(levelIndex, maxBoardSide);
        }
        return _generator.Generate(levelIndex, ComputeSeed(levelIndex), maxBoardSide);
    }

    private static int ComputeSeed(int levelIndex) => unchecked(levelIndex * 7919 + 42);
}
