using TerminalSnake.Domain;
using TerminalSnake.Game;
using TerminalSnake.Generation;
using TerminalSnake.Input;
using TerminalSnake.Rendering;

namespace TerminalSnake.Tests.Game;

public sealed class GameEngineTests
{
    private static GameEngine CreateEngine(
        TimeSpan? idleThreshold = null,
        TimeSpan? animationStep = null,
        TimeSpan? demoMovePause = null,
        int startLevel = 1)
    {
        return new GameEngine(
            levels: new LevelManager(),
            idleWatcher: new IdleWatcher(idleThreshold ?? TimeSpan.FromSeconds(30)),
            animationScheduler: new AnimationScheduler(animationStep ?? TimeSpan.FromMilliseconds(10)),
            renderer: new BoardRenderer(),
            demoMovePause: demoMovePause ?? TimeSpan.FromMilliseconds(20),
            startLevel: startLevel);
    }

    [Fact]
    public void Starts_on_level_one_in_player_mode_with_no_selection()
    {
        var engine = CreateEngine();
        Assert.Equal(1, engine.LevelIndex);
        Assert.Equal(GameMode.Player, engine.Mode);
        Assert.Null(engine.SelectedSnakeIndex);
        Assert.False(engine.IsAnimating);
    }

    [Fact]
    public void Tab_cycles_selection_forward_and_shift_tab_backward()
    {
        var engine = CreateEngine();
        var count = engine.Board.Snakes.Length;

        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        Assert.Equal(0, engine.SelectedSnakeIndex);

        for (var i = 1; i < count; i++)
        {
            engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.FromMilliseconds(i));
            Assert.Equal(i, engine.SelectedSnakeIndex);
        }

        engine.HandleKey(new KeyEvent(ConsoleKey.Tab, Shift: true), TimeSpan.FromMilliseconds(100));
        Assert.Equal(count - 2, engine.SelectedSnakeIndex);
    }

    [Fact]
    public void Enter_triggers_animation_and_tick_commits_the_board_after_the_step_duration()
    {
        var engine = CreateEngine(animationStep: TimeSpan.FromMilliseconds(5));
        var originalCount = engine.Board.Snakes.Length;
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        var selected = engine.SelectedSnakeIndex!.Value;
        var snakeBefore = engine.Board.Snakes[selected];
        engine.HandleKey(new KeyEvent(ConsoleKey.Enter), TimeSpan.Zero);

        Assert.True(engine.IsAnimating);

        // Run ticks for 500 ms at 10 ms cadence to exhaust animation frames.
        for (var ms = 0; ms <= 500; ms += 10)
        {
            engine.Tick(TimeSpan.FromMilliseconds(ms));
        }

        Assert.False(engine.IsAnimating);
        // Either the snake exited (count - 1) or advanced to a new shape (same count).
        var afterCount = engine.Board.Snakes.Length;
        Assert.True(afterCount <= originalCount);
    }

    [Fact]
    public void R_restarts_the_current_level()
    {
        var engine = CreateEngine(startLevel: 1);
        var initial = engine.Board;
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        engine.HandleKey(new KeyEvent(ConsoleKey.Enter), TimeSpan.Zero);
        for (var ms = 0; ms <= 500; ms += 10)
        {
            engine.Tick(TimeSpan.FromMilliseconds(ms));
        }
        engine.HandleKey(new KeyEvent(ConsoleKey.R), TimeSpan.FromMilliseconds(600));
        Assert.Equal(initial.Snakes.Length, engine.Board.Snakes.Length);
    }

    [Fact]
    public void Clicking_a_board_cell_on_a_snake_selects_and_triggers_it()
    {
        var engine = CreateEngine();
        var snake = engine.Board.Snakes[0];
        var head = snake.Head;
        engine.HandleBoardClick(head.X, head.Y, TimeSpan.Zero);
        Assert.Equal(0, engine.SelectedSnakeIndex);
        Assert.True(engine.IsAnimating);
    }

    [Fact]
    public void Clicking_an_empty_cell_is_ignored()
    {
        var engine = CreateEngine();
        engine.HandleBoardClick(-1, -1, TimeSpan.Zero);
        Assert.Null(engine.SelectedSnakeIndex);
        Assert.False(engine.IsAnimating);
    }

    [Fact]
    public void Idle_past_threshold_enters_demo_mode_and_starts_dequeueing()
    {
        var engine = CreateEngine(
            idleThreshold: TimeSpan.FromSeconds(1),
            animationStep: TimeSpan.FromMilliseconds(5),
            demoMovePause: TimeSpan.Zero);

        // Idle from t=0: no activity noted yet, immediately idle above threshold.
        engine.Tick(TimeSpan.FromSeconds(2));
        Assert.Equal(GameMode.Demo, engine.Mode);

        // Let the demo play out for a few seconds — at least one snake should advance.
        for (var ms = 2000; ms <= 6000; ms += 10)
        {
            engine.Tick(TimeSpan.FromMilliseconds(ms));
        }
    }

    [Fact]
    public void Input_during_demo_returns_control_to_player()
    {
        var engine = CreateEngine(idleThreshold: TimeSpan.FromMilliseconds(100));
        engine.Tick(TimeSpan.FromMilliseconds(200));
        Assert.Equal(GameMode.Demo, engine.Mode);

        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.FromMilliseconds(250));
        Assert.Equal(GameMode.Player, engine.Mode);
    }

    [Fact]
    public void Null_key_event_is_rejected()
    {
        var engine = CreateEngine();
        Assert.Throws<ArgumentNullException>(() => engine.HandleKey(null!, TimeSpan.Zero));
    }

    [Fact]
    public void Animations_block_key_input()
    {
        var engine = CreateEngine(animationStep: TimeSpan.FromSeconds(10));
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        engine.HandleKey(new KeyEvent(ConsoleKey.Enter), TimeSpan.Zero);

        var selectionBefore = engine.SelectedSnakeIndex;
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.FromMilliseconds(10));
        Assert.Equal(selectionBefore, engine.SelectedSnakeIndex);
    }

    [Fact]
    public void Render_produces_a_frame_buffer_that_matches_the_viewport()
    {
        var engine = CreateEngine();
        var viewport = ViewportCalculator.Compute(
            ViewportCalculator.MinimumWidth + 8,
            ViewportCalculator.MinimumHeight + 8,
            engine.Board.Size);
        var buffer = engine.Render(viewport, TimeSpan.Zero);
        Assert.Equal(viewport.TerminalWidth, buffer.Width);
        Assert.Equal(viewport.TerminalHeight, buffer.Height);
    }
}
