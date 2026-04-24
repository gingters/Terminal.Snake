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
    // The selection funnel is narrow near the origin — only the head column
    // plus one cell either side is reachable for the first NarrowDepth
    // rows — and widens by one cell per row after that. That stops a
    // closer-but-sideways snake from stealing the selection when a snake
    // is further away but right in front of the head. See issue #19 +
    // follow-up.
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
        int? bestIndex = null;
        var bestDistance = int.MaxValue;
        for (var i = 0; i < snakes.Count; i++)
        {
            if (i == excludedIndex)
            {
                continue;
            }
            var distance = ProjectedDistance(origin, snakes[i].Head, delta);
            if (distance is int d && d < bestDistance)
            {
                bestDistance = d;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private static int? ProjectedDistance(Cell origin, Cell target, Cell delta)
    {
        var dx = target.X - origin.X;
        var dy = target.Y - origin.Y;
        var projection = dx * delta.X + dy * delta.Y;
        if (projection <= 0)
        {
            return null;
        }
        var perpendicular = delta.X == 0 ? Math.Abs(dx) : Math.Abs(dy);
        var maxPerpendicular = Math.Max(NarrowRadius, projection - NarrowDepth);
        if (perpendicular > maxPerpendicular)
        {
            return null;
        }
        return Math.Abs(dx) + Math.Abs(dy);
    }
}
