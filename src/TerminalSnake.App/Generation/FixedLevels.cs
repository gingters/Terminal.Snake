using TerminalSnake.Domain;

namespace TerminalSnake.Generation;

public static class FixedLevels
{
    public const int Count = 10;

    // Hand-picked seeds that feed the generator's difficulty profile for each
    // tutorial level (1..10). The generator guarantees solvability before
    // returning, so these boards are curated tutorial content.
    private static readonly int[] Seeds =
    {
        11_001, 12_017, 13_042, 14_081, 15_120,
        16_213, 17_345, 18_511, 19_701, 20_903,
    };

    public static Board Get(int tutorialIndex)
    {
        if (tutorialIndex < 1 || tutorialIndex > Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tutorialIndex), tutorialIndex, $"Tutorial index must be in [1, {Count}]");
        }
        var generator = new BoardGenerator();
        return generator.Generate(tutorialIndex, Seeds[tutorialIndex - 1]);
    }

    public static IReadOnlyList<Board> All()
    {
        var generator = new BoardGenerator();
        var boards = new List<Board>(Count);
        for (var index = 1; index <= Count; index++)
        {
            boards.Add(generator.Generate(index, Seeds[index - 1]));
        }
        return boards;
    }
}
