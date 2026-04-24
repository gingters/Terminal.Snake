using TerminalSnake.Domain;
using TerminalSnake.Generation;
using TerminalSnake.Input;
using TerminalSnake.Movement;
using TerminalSnake.Rendering;

namespace TerminalSnake.Game;

public sealed class GameEngine
{
    private static readonly TimeSpan DefaultAnimationStep = TimeSpan.FromMilliseconds(80);
    private static readonly TimeSpan DefaultIdleThreshold = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultDemoMovePause = TimeSpan.FromMilliseconds(250);

    private readonly LevelManager _levels;
    private readonly AnimationScheduler _animation;
    private readonly IdleWatcher _idle;
    private readonly BoardRenderer _renderer;
    private readonly HudRenderer _hudRenderer;
    private readonly HudStrings _hudStrings;
    private readonly TimeSpan _demoMovePause;

    private Board _currentBoard;
    private Board? _pendingBoardAfterAnimation;
    private int _preAnimationSnakeCount;
    private Queue<int> _demoQueue = new();
    private TimeSpan _lastDemoMoveAt = TimeSpan.Zero;

    public GameEngine(
        LevelManager? levels = null,
        IdleWatcher? idleWatcher = null,
        AnimationScheduler? animationScheduler = null,
        BoardRenderer? renderer = null,
        HudRenderer? hudRenderer = null,
        HudStrings? hudStrings = null,
        TimeSpan? demoMovePause = null,
        int startLevel = 1)
    {
        _levels = Or(levels, DefaultLevels);
        _idle = Or(idleWatcher, DefaultIdle);
        _animation = Or(animationScheduler, DefaultAnimation);
        _renderer = Or(renderer, DefaultBoardRenderer);
        _hudRenderer = Or(hudRenderer, DefaultHudRenderer);
        _hudStrings = Or(hudStrings, DefaultHudStrings);
        _demoMovePause = demoMovePause ?? DefaultDemoMovePause;
        LevelIndex = startLevel;
        _currentBoard = _levels.LoadLevel(startLevel);
    }

    private static T Or<T>(T? value, Func<T> fallback) where T : class
        => value ?? fallback();

    private static LevelManager DefaultLevels() => new();
    private static IdleWatcher DefaultIdle() => new(DefaultIdleThreshold);
    private static AnimationScheduler DefaultAnimation() => new(DefaultAnimationStep);
    private static BoardRenderer DefaultBoardRenderer() => new();
    private static HudRenderer DefaultHudRenderer() => new();
    private static HudStrings DefaultHudStrings() => HudLocalization.Default;

    public int LevelIndex { get; private set; }

    public GameMode Mode { get; private set; } = GameMode.Player;

    public int? SelectedSnakeIndex { get; private set; }

    public Board Board => _currentBoard;

    public bool IsAnimating => _animation.IsBusy;

    // Starts true so the legend is visible at game start and during demo
    // playback. Any gameplay-affecting input (Tab, Enter, Space, R, a click)
    // flips it off; H toggles it at will. See issue #15.
    public bool HelpVisible { get; private set; } = true;

    // Starts true so auto-play kicks in after the idle threshold on a fresh
    // game. The first gameplay input turns it off permanently (until the
    // player presses D to re-arm it), so stepping away for a coffee on
    // level 4 does not cause level 5 to start playing itself. See issue #16.
    public bool AutoPlayEnabled { get; private set; } = true;

    public void HandleKey(KeyEvent key, TimeSpan now)
    {
        ArgumentNullException.ThrowIfNull(key);
        NoteInputActivity(now);
        if (_animation.IsBusy)
        {
            return;
        }
        DispatchKey(key, now);
    }

    public void HandleBoardClick(int boardX, int boardY, TimeSpan now)
    {
        NoteInputActivity(now);
        HelpVisible = false;
        AutoPlayEnabled = false;
        if (_animation.IsBusy)
        {
            return;
        }
        for (var i = 0; i < _currentBoard.Snakes.Length; i++)
        {
            if (ContainsBoardPoint(_currentBoard.Snakes[i], boardX, boardY))
            {
                SelectedSnakeIndex = i;
                TriggerSnake(i, now);
                return;
            }
        }
    }

    private void NoteInputActivity(TimeSpan now)
    {
        if (Mode == GameMode.Demo)
        {
            ExitDemoMode();
        }
        _idle.NoteActivity(now);
    }

    public void Tick(TimeSpan now)
    {
        TickAnimationCompletion(now);
        TickLevelTransition();
        TickDemoModeTransition(now);
        TickDemoPlayback(now);
    }

    private void TickAnimationCompletion(TimeSpan now)
    {
        if (_animation.IsBusy && _animation.IsComplete(now))
        {
            FinalizeAnimation();
        }
    }

    private void TickLevelTransition()
    {
        if (!_animation.IsBusy && _currentBoard.Snakes.Length == 0)
        {
            AdvanceToNextLevel();
        }
    }

    private void TickDemoModeTransition(TimeSpan now)
    {
        if (AutoPlayEnabled && Mode == GameMode.Player && _idle.HasIdledOut(now))
        {
            EnterDemoMode();
        }
    }

    private void TickDemoPlayback(TimeSpan now)
    {
        if (Mode == GameMode.Demo && !_animation.IsBusy)
        {
            TryDequeueDemoMove(now);
        }
    }

    public FrameBuffer Render(Viewport viewport, TimeSpan now)
    {
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        var (visibleBoard, overlay) = ApplyAnimationSnapshot(_currentBoard, now);
        _renderer.Render(buffer, visibleBoard, viewport, SelectedSnakeIndex, overlay);
        _hudRenderer.Render(buffer, viewport, new HudModel(LevelIndex, Mode, HelpVisible, _hudStrings));
        return buffer;
    }

    /// <summary>
    /// Builds a viewport that always matches the engine's current board size.
    /// Callers that hang on to a cached viewport across level transitions can
    /// end up passing a viewport whose <c>BoardSide</c> no longer matches the
    /// new board, which the renderer rejects — the crash reported in issue #7.
    /// Callers should invoke this on every render instead of caching.
    /// </summary>
    public Viewport BuildViewport(int terminalWidth, int terminalHeight)
    {
        return ViewportCalculator.Compute(
            Math.Max(terminalWidth, ViewportCalculator.MinimumWidth),
            Math.Max(terminalHeight, ViewportCalculator.MinimumHeight),
            _currentBoard.Size);
    }

    private void DispatchKey(KeyEvent key, TimeSpan now)
    {
        if (HandleMetaKey(key))
        {
            return;
        }
        HelpVisible = false;
        AutoPlayEnabled = false;
        DispatchGameplayKey(key, now);
    }

    private bool HandleMetaKey(KeyEvent key)
    {
        if (key.Key == ConsoleKey.H)
        {
            HelpVisible = !HelpVisible;
            return true;
        }
        if (key.Key == ConsoleKey.D)
        {
            AutoPlayEnabled = true;
            return true;
        }
        return false;
    }

    private void DispatchGameplayKey(KeyEvent key, TimeSpan now)
    {
        if (key.Key == ConsoleKey.Tab)
        {
            CycleSelection(key.Shift ? -1 : +1);
            return;
        }
        if (IsTriggerKey(key.Key))
        {
            TriggerSelectedSnake(now);
            return;
        }
        if (key.Key == ConsoleKey.R)
        {
            RestartLevel();
        }
    }

    private static bool IsTriggerKey(ConsoleKey key) =>
        key == ConsoleKey.Enter || key == ConsoleKey.Spacebar;

    private static bool ContainsBoardPoint(Snake snake, int boardX, int boardY)
    {
        foreach (var segment in snake.Segments)
        {
            if (segment.X == boardX && segment.Y == boardY)
            {
                return true;
            }
        }
        return false;
    }

    private void CycleSelection(int direction)
    {
        if (_currentBoard.Snakes.Length == 0)
        {
            SelectedSnakeIndex = null;
            return;
        }
        var count = _currentBoard.Snakes.Length;
        var next = (SelectedSnakeIndex ?? -1) + direction;
        SelectedSnakeIndex = ((next % count) + count) % count;
    }

    private void TriggerSelectedSnake(TimeSpan now)
    {
        if (SelectedSnakeIndex is int index)
        {
            TriggerSnake(index, now);
        }
    }

    private void TriggerSnake(int snakeIndex, TimeSpan now)
    {
        if (snakeIndex < 0 || snakeIndex >= _currentBoard.Snakes.Length)
        {
            return;
        }
        var outcome = MoveEngine.Advance(_currentBoard, snakeIndex);
        if (outcome.Steps == 0)
        {
            return;
        }
        _preAnimationSnakeCount = _currentBoard.Snakes.Length;
        _pendingBoardAfterAnimation = outcome.ResultingBoard;
        _animation.Start(snakeIndex, outcome.Frames, now);
    }

    private void FinalizeAnimation()
    {
        var snakeExited = CommitPendingBoard();
        _animation.Clear();
        UpdateSelectionAfterFinalize(snakeExited);
    }

    private bool CommitPendingBoard()
    {
        if (_pendingBoardAfterAnimation is null)
        {
            return false;
        }
        var exited = _pendingBoardAfterAnimation.Snakes.Length < _preAnimationSnakeCount;
        _currentBoard = _pendingBoardAfterAnimation;
        _pendingBoardAfterAnimation = null;
        return exited;
    }

    private void UpdateSelectionAfterFinalize(bool snakeExited)
    {
        if (snakeExited)
        {
            // Clear the selection after an exit so the player actively picks
            // the next snake instead of hitting Enter until the board empties
            // — see issue #14.
            SelectedSnakeIndex = null;
            return;
        }
        if (SelectedSnakeIndex is int selected && selected >= _currentBoard.Snakes.Length)
        {
            SelectedSnakeIndex = _currentBoard.Snakes.Length > 0 ? 0 : null;
        }
    }

    private void RestartLevel()
    {
        _currentBoard = _levels.LoadLevel(LevelIndex);
        _pendingBoardAfterAnimation = null;
        _animation.Clear();
        _demoQueue.Clear();
        SelectedSnakeIndex = null;
    }

    private void AdvanceToNextLevel()
    {
        LevelIndex += 1;
        _currentBoard = _levels.LoadLevel(LevelIndex);
        SelectedSnakeIndex = null;
        Mode = GameMode.Player;
        _demoQueue.Clear();
    }

    private void EnterDemoMode()
    {
        Mode = GameMode.Demo;
        SelectedSnakeIndex = null;
        HelpVisible = true;
        var solution = Solver.TrySolve(_currentBoard);
        _demoQueue = solution is null
            ? new Queue<int>()
            : new Queue<int>(solution);
        _lastDemoMoveAt = TimeSpan.Zero;
    }

    private void ExitDemoMode()
    {
        Mode = GameMode.Player;
        _demoQueue.Clear();
    }

    private void TryDequeueDemoMove(TimeSpan now)
    {
        if (_demoQueue.Count == 0)
        {
            return;
        }
        if (now - _lastDemoMoveAt < _demoMovePause)
        {
            return;
        }
        var index = _demoQueue.Dequeue();
        TriggerSnake(index, now);
        _lastDemoMoveAt = now;
    }

    private (Board Board, IReadOnlyDictionary<Cell, SnakeColor>? Overlay)
        ApplyAnimationSnapshot(Board board, TimeSpan now)
    {
        var snapshot = _animation.Current(now);
        if (snapshot is null)
        {
            return (board, null);
        }

        var original = board.Snakes[snapshot.Value.SnakeIndex];
        if (snapshot.Value.Segments.Length >= 2)
        {
            var replaced = new Snake(snapshot.Value.Segments, original.Color);
            var updated = board.Snakes.SetItem(snapshot.Value.SnakeIndex, replaced);
            return (board.WithSnakes(updated), null);
        }

        // Final frames of an exit animation: the snake has shrunk below the
        // two-segment minimum Snake requires for a valid direction. Drop it
        // from the board and paint the leftover segment (if any) as a plain
        // overlay cell so the tail still animates cell-by-cell off-screen.
        var trimmedSnakes = board.Snakes.RemoveAt(snapshot.Value.SnakeIndex);
        var trimmedBoard = board.WithSnakes(trimmedSnakes);
        if (snapshot.Value.Segments.Length == 1)
        {
            var overlay = new Dictionary<Cell, SnakeColor>
            {
                [snapshot.Value.Segments[0]] = original.Color,
            };
            return (trimmedBoard, overlay);
        }
        return (trimmedBoard, null);
    }
}
