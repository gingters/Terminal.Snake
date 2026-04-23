using System.Collections.Immutable;
using TerminalSnake.Domain;

namespace TerminalSnake.Movement;

public sealed record MoveOutcome(
    Board ResultingBoard,
    int SnakeIndex,
    int Steps,
    bool Exited,
    int? BlockedBy,
    ImmutableArray<ImmutableArray<Cell>> Frames)
{
    public bool Moved => Steps > 0;
}
