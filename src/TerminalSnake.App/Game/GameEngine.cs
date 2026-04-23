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
    private readonly TimeSpan _demoMovePause;

    private Board _currentBoard;
    private Board? _pendingBoardAfterAnimation;
    private Queue<int> _demoQueue = new();
    private TimeSpan _lastDemoMoveAt = TimeSpan.Zero;

    public GameEngine(
        LevelManager? levels = null,
        IdleWatcher? idleWatcher = null,
        AnimationScheduler? animationScheduler = null,
        BoardRenderer? renderer = null,
        TimeSpan? demoMovePause = null,
        int startLevel = 1)
    {
        _levels = levels ?? new LevelManager();
        _idle = idleWatcher ?? new IdleWatcher(DefaultIdleThreshold);
        _animation = animationScheduler ?? new AnimationScheduler(DefaultAnimationStep);
        _renderer = renderer ?? new BoardRenderer();
        _demoMovePause = demoMovePause ?? DefaultDemoMovePause;
        LevelIndex = startLevel;
        _currentBoard = _levels.LoadLevel(startLevel);
    }

    public int LevelIndex { get; private set; }

    public GameMode Mode { get; private set; } = GameMode.Player;

    public int? SelectedSnakeIndex { get; private set; }

    public Board Board => _currentBoard;

    public bool IsAnimating => _animation.IsBusy;

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
        if (_animation.IsBusy && _animation.IsComplete(now))
        {
            FinalizeAnimation();
        }
        if (!_animation.IsBusy && _currentBoard.Snakes.Length == 0)
        {
            AdvanceToNextLevel();
        }
        if (Mode == GameMode.Player && _idle.HasIdledOut(now))
        {
            EnterDemoMode();
        }
        if (Mode == GameMode.Demo && !_animation.IsBusy)
        {
            TryDequeueDemoMove(now);
        }
    }

    public FrameBuffer Render(Viewport viewport, TimeSpan now)
    {
        var buffer = new FrameBuffer(viewport.TerminalWidth, viewport.TerminalHeight);
        var visibleBoard = ApplyAnimationSnapshot(_currentBoard, now);
        _renderer.Render(buffer, visibleBoard, viewport, SelectedSnakeIndex);
        return buffer;
    }

    private void DispatchKey(KeyEvent key, TimeSpan now)
    {
        switch (key.Key)
        {
            case ConsoleKey.Tab when key.Shift:
                CycleSelection(-1);
                break;
            case ConsoleKey.Tab:
                CycleSelection(+1);
                break;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                TriggerSelectedSnake(now);
                break;
            case ConsoleKey.R:
                RestartLevel();
                break;
        }
    }

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
        _pendingBoardAfterAnimation = outcome.ResultingBoard;
        _animation.Start(snakeIndex, outcome.Frames, now);
    }

    private void FinalizeAnimation()
    {
        if (_pendingBoardAfterAnimation is not null)
        {
            _currentBoard = _pendingBoardAfterAnimation;
            _pendingBoardAfterAnimation = null;
        }
        _animation.Clear();
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

    private Board ApplyAnimationSnapshot(Board board, TimeSpan now)
    {
        var snapshot = _animation.Current(now);
        if (snapshot is null)
        {
            return board;
        }
        var original = board.Snakes[snapshot.Value.SnakeIndex];
        var replaced = new Snake(snapshot.Value.Segments, original.Color);
        var updated = board.Snakes.SetItem(snapshot.Value.SnakeIndex, replaced);
        return board.WithSnakes(updated);
    }
}
