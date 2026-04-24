using TerminalSnake.Domain;

namespace TerminalSnake.Game;

/// <summary>
/// Generates the next level in a background task while the player is
/// still busy with the current one. On level-up the engine takes the
/// pre-built board instead of running the (now very slow) partial-move
/// solver on the game loop. Issue #38: dense, genuinely challenging
/// boards cost 1-3 s to verify, which only works if that work lands
/// off the render/input thread.
/// </summary>
public sealed class LevelPrefetcher
{
    private readonly Func<int, int, Board> _loadSynchronously;
    private Task<Board>? _pending;
    private int _pendingLevel;
    private int _pendingMaxBoardSide;

    public LevelPrefetcher(Func<int, int, Board> loadSynchronously)
    {
        ArgumentNullException.ThrowIfNull(loadSynchronously);
        _loadSynchronously = loadSynchronously;
    }

    public bool IsPreparing(int level, int maxBoardSide) =>
        _pending is not null
        && _pendingLevel == level
        && _pendingMaxBoardSide == maxBoardSide
        && !_pending.IsCompleted;

    public bool IsReady(int level, int maxBoardSide) =>
        _pending is not null
        && _pendingLevel == level
        && _pendingMaxBoardSide == maxBoardSide
        && _pending.IsCompletedSuccessfully;

    /// <summary>
    /// Kicks off a background generation for the requested level.
    /// Replaces any in-flight prefetch for a different level — the old
    /// task is left running and its result dropped, since cancelling
    /// mid-BFS isn't worth the plumbing.
    /// </summary>
    public void PreparationRequested(int level, int maxBoardSide)
    {
        if (_pending is not null && _pendingLevel == level && _pendingMaxBoardSide == maxBoardSide)
        {
            return;
        }
        _pendingLevel = level;
        _pendingMaxBoardSide = maxBoardSide;
        _pending = Task.Run(() => _loadSynchronously(level, maxBoardSide));
    }

    /// <summary>
    /// Returns the pre-built board for the requested level. Blocks if
    /// the background generation hasn't finished yet — callers that
    /// care (the live game loop) should check IsReady first and show
    /// a "preparing next level" indicator while they wait.
    /// </summary>
    public Board Take(int level, int maxBoardSide)
    {
        if (_pending is null || _pendingLevel != level || _pendingMaxBoardSide != maxBoardSide)
        {
            // No matching prefetch — run inline. Shouldn't happen on
            // the standard play path since the engine always requests
            // the next level right after loading the current one.
            return _loadSynchronously(level, maxBoardSide);
        }
        var board = _pending.GetAwaiter().GetResult();
        _pending = null;
        return board;
    }
}
