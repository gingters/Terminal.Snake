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
    // Shorter than the cold-start idle threshold: if the user explicitly
    // re-arms auto-play with D, the demo should kick off quickly (#16).
    private static readonly TimeSpan DefaultDemoArmDelay = TimeSpan.FromSeconds(1);

    private readonly LevelManager _levels;
    private readonly AnimationScheduler _animation;
    private readonly IdleWatcher _idle;
    private readonly BoardRenderer _renderer;
    private readonly HudRenderer _hudRenderer;
    private readonly HudStrings _hudStrings;
    private readonly TimeSpan _demoMovePause;
    private readonly TimeSpan _demoArmDelay;

    private Board _currentBoard;
    private Board? _pendingBoardAfterAnimation;
    private int _preAnimationSnakeCount;
    private Queue<int> _demoQueue = new();
    private TimeSpan _lastDemoMoveAt = TimeSpan.Zero;
    private TimeSpan? _demoArmedAt;

    public GameEngine(
        LevelManager? levels = null,
        IdleWatcher? idleWatcher = null,
        AnimationScheduler? animationScheduler = null,
        BoardRenderer? renderer = null,
        HudRenderer? hudRenderer = null,
        HudStrings? hudStrings = null,
        TimeSpan? demoMovePause = null,
        TimeSpan? demoArmDelay = null,
        int startLevel = 1)
    {
        _levels = Or(levels, DefaultLevels);
        _idle = Or(idleWatcher, DefaultIdle);
        _animation = Or(animationScheduler, DefaultAnimation);
        _renderer = Or(renderer, DefaultBoardRenderer);
        _hudRenderer = Or(hudRenderer, DefaultHudRenderer);
        _hudStrings = Or(hudStrings, DefaultHudStrings);
        _demoMovePause = demoMovePause ?? DefaultDemoMovePause;
        _demoArmDelay = demoArmDelay ?? DefaultDemoArmDelay;
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
        _demoArmedAt = null;
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
        if (!AutoPlayEnabled || Mode != GameMode.Player)
        {
            return;
        }
        if (ShouldEnterDemo(now))
        {
            EnterDemoMode();
        }
    }

    private bool ShouldEnterDemo(TimeSpan now)
    {
        // An explicit D re-arm starts a short fuse that ignores the normal
        // 30-second idle threshold — the player just asked for auto-play.
        if (_demoArmedAt is { } armedAt && now - armedAt >= _demoArmDelay)
        {
            return true;
        }
        return _idle.HasIdledOut(now);
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
        var (visibleBoard, overlay, effectiveSelection) = ApplyAnimationSnapshot(_currentBoard, now);
        _renderer.Render(buffer, visibleBoard, viewport, effectiveSelection, overlay);
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
        if (HandleMetaKey(key, now))
        {
            return;
        }
        HelpVisible = false;
        AutoPlayEnabled = false;
        _demoArmedAt = null;
        DispatchGameplayKey(key, now);
    }

    private bool HandleMetaKey(KeyEvent key, TimeSpan now)
    {
        if (key.Key == ConsoleKey.H)
        {
            HelpVisible = !HelpVisible;
            return true;
        }
        if (key.Key == ConsoleKey.D)
        {
            AutoPlayEnabled = true;
            _demoArmedAt = now;
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
        if (TryMapArrowToDirection(key.Key) is Direction direction)
        {
            SelectNearestSnakeInDirection(direction);
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
            return;
        }
        if (TryMapDigitToLevel(key.Key) is int jumpLevel)
        {
            JumpToLevel(jumpLevel);
        }
    }

    // Digit keys 1..9 jump to the matching tutorial level, 0 jumps to
    // level 10 — the end of the handcrafted levels, which is also the
    // jumping-off point into the procedural generator. Lets the player
    // skip the easy intro levels when they just want harder puzzles
    // (#25). Returns null for any non-digit key.
    private static int? TryMapDigitToLevel(ConsoleKey key)
    {
        if (key >= ConsoleKey.D1 && key <= ConsoleKey.D9)
        {
            return key - ConsoleKey.D0;
        }
        if (key == ConsoleKey.D0)
        {
            return 10;
        }
        return null;
    }

    private void JumpToLevel(int levelIndex)
    {
        LevelIndex = levelIndex;
        _currentBoard = _levels.LoadLevel(levelIndex);
        _pendingBoardAfterAnimation = null;
        _animation.Clear();
        _demoQueue.Clear();
        SelectedSnakeIndex = null;
    }

    private static Direction? TryMapArrowToDirection(ConsoleKey key) => key switch
    {
        ConsoleKey.UpArrow => Direction.Up,
        ConsoleKey.DownArrow => Direction.Down,
        ConsoleKey.LeftArrow => Direction.Left,
        ConsoleKey.RightArrow => Direction.Right,
        _ => null,
    };

    private void SelectNearestSnakeInDirection(Direction direction)
    {
        if (_currentBoard.Snakes.Length == 0)
        {
            return;
        }
        var origin = SelectedSnakeIndex is int idx
            ? _currentBoard.Snakes[idx].Head
            : new Cell(_currentBoard.Size / 2, _currentBoard.Size / 2);
        var nearest = SnakeSelection.FindNearestInDirection(
            _currentBoard.Snakes, origin, direction, SelectedSnakeIndex);
        if (nearest is int nextIndex)
        {
            SelectedSnakeIndex = nextIndex;
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
        _demoQueue.Clear();

        if (Mode == GameMode.Demo)
        {
            // If auto-play finished the previous level, keep running on the
            // next one without waiting for the idle threshold again — the
            // user asked for auto-play and hasn't taken over. Reload the
            // solver for the fresh board and reset the dequeue pacing.
            ReloadDemoQueueForCurrentBoard();
            _lastDemoMoveAt = TimeSpan.Zero;
            HelpVisible = true;
            return;
        }

        Mode = GameMode.Player;
    }

    private void ReloadDemoQueueForCurrentBoard()
    {
        var solution = Solver.TrySolve(_currentBoard);
        if (solution is not null)
        {
            _demoQueue = new Queue<int>(solution);
        }
    }

    private void EnterDemoMode()
    {
        Mode = GameMode.Demo;
        SelectedSnakeIndex = null;
        HelpVisible = true;
        _demoArmedAt = null;
        _demoQueue.Clear();
        ReloadDemoQueueForCurrentBoard();
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

    private (Board Board, IReadOnlyDictionary<Cell, SnakeColor>? Overlay, int? Selection)
        ApplyAnimationSnapshot(Board board, TimeSpan now)
    {
        var snapshot = _animation.Current(now);
        if (snapshot is null)
        {
            return (board, null, SelectedSnakeIndex);
        }

        var original = board.Snakes[snapshot.Value.SnakeIndex];
        if (snapshot.Value.Segments.Length >= 2)
        {
            var replaced = new Snake(snapshot.Value.Segments, original.Color);
            var updated = board.Snakes.SetItem(snapshot.Value.SnakeIndex, replaced);
            return (board.WithSnakes(updated), null, SelectedSnakeIndex);
        }

        // Final frames of an exit animation: the snake has shrunk below the
        // two-segment minimum Snake requires for a valid direction. Drop it
        // from the board and paint the leftover segment (if any) as a plain
        // overlay cell so the tail still animates cell-by-cell off-screen.
        // Suppress the selection highlight too — the exiting snake's slot
        // has been filled by whichever snake shifted down, and highlighting
        // *that* one for a couple of frames before FinalizeAnimation clears
        // the selection produces a flicker (#29).
        var trimmedSnakes = board.Snakes.RemoveAt(snapshot.Value.SnakeIndex);
        var trimmedBoard = board.WithSnakes(trimmedSnakes);
        if (snapshot.Value.Segments.Length == 1)
        {
            var overlay = new Dictionary<Cell, SnakeColor>
            {
                [snapshot.Value.Segments[0]] = original.Color,
            };
            return (trimmedBoard, overlay, null);
        }
        return (trimmedBoard, null, null);
    }
}
