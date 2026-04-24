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
        // Keep the candidate within the direction's "cone": the move-axis
        // component must dominate the perpendicular, so pressing Right on
        // (2,2) with a snake at (3,9) doesn't jump the selection across the
        // board just because the snake is technically to the right of the
        // origin column.
        var perpendicular = delta.X == 0 ? Math.Abs(dx) : Math.Abs(dy);
        if (perpendicular > projection)
        {
            return null;
        }
        return Math.Abs(dx) + Math.Abs(dy);
    }
}
