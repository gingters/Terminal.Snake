using TerminalSnake.Domain;

namespace TerminalSnake.Game;

/// <summary>
/// Helpers for "find the nearest snake in a given compass direction" —
/// used by <see cref="GameEngine"/> when the player hits an arrow key
/// to jump the selection through the board instead of tabbing through
/// the snake list sequentially (issue #19).
/// </summary>
public static class SnakeSelection
{
    // Two-stage picker: first try the narrow funnel in front of the origin
    // — head column plus one cell either side up to NarrowDepth, widening
    // by one cell per row after that — so a snake straight in front wins
    // over a closer-but-sideways candidate. If nothing is in the funnel,
    // fall back to the full half-plane (any snake on the "forward" side)
    // by Manhattan distance, so sparse/corner layouts stay navigable even
    // when every other snake is off-axis.
    private const int NarrowRadius = 1;
    private const int NarrowDepth = 3;

    public static int? FindNearestInDirection(
        IReadOnlyList<Snake> snakes,
        Cell origin,
        Direction direction,
        int? excludedIndex)
    {
        ArgumentNullException.ThrowIfNull(snakes);
        var delta = direction.Delta();
        int? narrowBest = null;
        var narrowDistance = int.MaxValue;
        int? wideBest = null;
        var wideDistance = int.MaxValue;
        for (var i = 0; i < snakes.Count; i++)
        {
            if (i == excludedIndex)
            {
                continue;
            }
            EvaluateCandidate(origin, snakes[i].Head, delta, i,
                ref narrowBest, ref narrowDistance,
                ref wideBest, ref wideDistance);
        }
        return narrowBest ?? wideBest;
    }

    private static void EvaluateCandidate(
        Cell origin, Cell target, Cell delta, int index,
        ref int? narrowBest, ref int narrowDistance,
        ref int? wideBest, ref int wideDistance)
    {
        var dx = target.X - origin.X;
        var dy = target.Y - origin.Y;
        var projection = dx * delta.X + dy * delta.Y;
        if (projection <= 0)
        {
            return;
        }
        var manhattan = Math.Abs(dx) + Math.Abs(dy);
        if (manhattan < wideDistance)
        {
            wideDistance = manhattan;
            wideBest = index;
        }
        var perpendicular = delta.X == 0 ? Math.Abs(dx) : Math.Abs(dy);
        var maxPerpendicular = Math.Max(NarrowRadius, projection - NarrowDepth);
        if (perpendicular <= maxPerpendicular && manhattan < narrowDistance)
        {
            narrowDistance = manhattan;
            narrowBest = index;
        }
    }
}
