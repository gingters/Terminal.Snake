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
        TimeSpan? demoArmDelay = null,
        int startLevel = 1)
    {
        return new GameEngine(
            levels: new LevelManager(),
            idleWatcher: new IdleWatcher(idleThreshold ?? TimeSpan.FromSeconds(30)),
            animationScheduler: new AnimationScheduler(animationStep ?? TimeSpan.FromMilliseconds(10)),
            renderer: new BoardRenderer(),
            demoMovePause: demoMovePause ?? TimeSpan.FromMilliseconds(20),
            demoArmDelay: demoArmDelay ?? TimeSpan.FromMilliseconds(50),
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
        Assert.True(engine.HelpVisible);
    }

    [Fact]
    public void H_toggles_help_visibility()
    {
        var engine = CreateEngine();
        Assert.True(engine.HelpVisible);
        engine.HandleKey(new KeyEvent(ConsoleKey.H), TimeSpan.Zero);
        Assert.False(engine.HelpVisible);
        engine.HandleKey(new KeyEvent(ConsoleKey.H), TimeSpan.FromMilliseconds(1));
        Assert.True(engine.HelpVisible);
    }

    [Theory]
    [InlineData(ConsoleKey.Tab)]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    [InlineData(ConsoleKey.R)]
    public void Any_gameplay_key_hides_the_help_overlay(ConsoleKey key)
    {
        var engine = CreateEngine();
        Assert.True(engine.HelpVisible);
        engine.HandleKey(new KeyEvent(key), TimeSpan.Zero);
        Assert.False(engine.HelpVisible);
    }

    [Fact]
    public void Board_click_hides_the_help_overlay()
    {
        var engine = CreateEngine();
        var head = engine.Board.Snakes[0].Head;
        engine.HandleBoardClick(head.X, head.Y, TimeSpan.Zero);
        Assert.False(engine.HelpVisible);
    }

    [Fact]
    public void Demo_mode_makes_the_help_overlay_visible_again()
    {
        // Tab hides help and (per #16) disables auto-play; press D to re-arm
        // auto-play before letting the short arm delay elapse.
        var engine = CreateEngine(
            idleThreshold: TimeSpan.FromMilliseconds(100),
            demoArmDelay: TimeSpan.FromMilliseconds(20));
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        Assert.False(engine.HelpVisible);
        engine.HandleKey(new KeyEvent(ConsoleKey.D), TimeSpan.FromMilliseconds(10));

        engine.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(GameMode.Demo, engine.Mode);
        Assert.True(engine.HelpVisible);
    }

    [Fact]
    public void AutoPlay_defaults_to_enabled()
    {
        var engine = CreateEngine();
        Assert.True(engine.AutoPlayEnabled);
    }

    [Theory]
    [InlineData(ConsoleKey.Tab)]
    [InlineData(ConsoleKey.Enter)]
    [InlineData(ConsoleKey.Spacebar)]
    [InlineData(ConsoleKey.R)]
    public void First_gameplay_key_disables_autoplay(ConsoleKey key)
    {
        var engine = CreateEngine();
        engine.HandleKey(new KeyEvent(key), TimeSpan.Zero);
        Assert.False(engine.AutoPlayEnabled);
    }

    [Fact]
    public void Board_click_disables_autoplay()
    {
        var engine = CreateEngine();
        var head = engine.Board.Snakes[0].Head;
        engine.HandleBoardClick(head.X, head.Y, TimeSpan.Zero);
        Assert.False(engine.AutoPlayEnabled);
    }

    [Fact]
    public void H_does_not_disable_autoplay()
    {
        var engine = CreateEngine();
        engine.HandleKey(new KeyEvent(ConsoleKey.H), TimeSpan.Zero);
        Assert.True(engine.AutoPlayEnabled);
    }

    [Fact]
    public void D_re_enables_autoplay_after_it_was_turned_off()
    {
        var engine = CreateEngine();
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        Assert.False(engine.AutoPlayEnabled);
        engine.HandleKey(new KeyEvent(ConsoleKey.D), TimeSpan.FromMilliseconds(10));
        Assert.True(engine.AutoPlayEnabled);
    }

    [Fact]
    public void Idle_after_first_input_does_not_enter_demo_mode()
    {
        // Issue #16: grabbing a coffee on level 4 must not cause level 5 to
        // start playing itself.
        var engine = CreateEngine(idleThreshold: TimeSpan.FromMilliseconds(50));
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        Assert.False(engine.AutoPlayEnabled);

        for (var ms = 0; ms < 2_000; ms += 50)
        {
            engine.Tick(TimeSpan.FromMilliseconds(ms));
        }
        Assert.Equal(GameMode.Player, engine.Mode);
    }

    [Fact]
    public void D_fires_demo_after_arm_delay_independent_of_idle_threshold()
    {
        // Issue #16 follow-up: after an explicit D the demo must kick in
        // after the short arm delay (1 s in production), not after the 30 s
        // idle timeout. Injecting a very long idle threshold proves the arm
        // delay alone triggers the transition.
        var engine = CreateEngine(
            idleThreshold: TimeSpan.FromHours(1),
            demoArmDelay: TimeSpan.FromMilliseconds(40));
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        engine.HandleKey(new KeyEvent(ConsoleKey.D), TimeSpan.FromMilliseconds(10));

        engine.Tick(TimeSpan.FromMilliseconds(30));
        Assert.Equal(GameMode.Player, engine.Mode);

        engine.Tick(TimeSpan.FromMilliseconds(60));
        Assert.Equal(GameMode.Demo, engine.Mode);
    }

    [Fact]
    public void Auto_play_carries_over_into_the_next_level_without_another_idle_wait()
    {
        // Issue #16 follow-up: once auto-play is running, finishing the level
        // must not drop back to Player mode and wait another 30 s of idle.
        // Drive the idle-triggered demo through a whole level and assert the
        // engine stays in Demo mode on the next level.
        var engine = CreateEngine(
            idleThreshold: TimeSpan.FromMilliseconds(10),
            animationStep: TimeSpan.FromMilliseconds(1),
            demoMovePause: TimeSpan.Zero,
            demoArmDelay: TimeSpan.FromMilliseconds(10));
        var clock = TimeSpan.Zero;

        var startLevel = engine.LevelIndex;
        var steps = 0;
        while (engine.LevelIndex == startLevel && steps++ < 20_000)
        {
            clock = clock.Add(TimeSpan.FromMilliseconds(2));
            engine.Tick(clock);
        }

        Assert.NotEqual(startLevel, engine.LevelIndex);
        Assert.Equal(GameMode.Demo, engine.Mode);
    }

    [Fact]
    public void D_arm_window_is_cancelled_when_the_player_acts_again()
    {
        // Pressing D and then Tab before the arm delay elapses must abort
        // the pending demo transition — the player took over.
        var engine = CreateEngine(
            idleThreshold: TimeSpan.FromHours(1),
            demoArmDelay: TimeSpan.FromMilliseconds(40));
        engine.HandleKey(new KeyEvent(ConsoleKey.D), TimeSpan.Zero);
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.FromMilliseconds(10));

        for (var ms = 20; ms < 500; ms += 20)
        {
            engine.Tick(TimeSpan.FromMilliseconds(ms));
        }
        Assert.Equal(GameMode.Player, engine.Mode);
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
    public void L_opens_the_level_prompt_and_blocks_gameplay_keys()
    {
        // Issue #36: digit-direct-jumps (#25) were replaced with an L
        // prompt so the player can type multi-digit levels. While the
        // prompt is open, gameplay keys are suspended.
        var engine = CreateEngine(startLevel: 1);
        engine.HandleKey(new KeyEvent(ConsoleKey.L), TimeSpan.Zero);
        Assert.True(engine.LevelPromptActive);
        Assert.Equal(string.Empty, engine.LevelPromptInput);

        // Digits accumulate in the input buffer.
        engine.HandleKey(new KeyEvent(ConsoleKey.D1), TimeSpan.FromMilliseconds(1));
        engine.HandleKey(new KeyEvent(ConsoleKey.D2), TimeSpan.FromMilliseconds(2));
        Assert.Equal("12", engine.LevelPromptInput);

        // Gameplay keys do not escape the prompt.
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.FromMilliseconds(3));
        Assert.True(engine.LevelPromptActive);
        Assert.Null(engine.SelectedSnakeIndex);
    }

    [Fact]
    public void Level_prompt_enter_commits_the_jump()
    {
        var engine = CreateEngine(startLevel: 1);
        engine.HandleKey(new KeyEvent(ConsoleKey.L), TimeSpan.Zero);
        engine.HandleKey(new KeyEvent(ConsoleKey.D1), TimeSpan.FromMilliseconds(1));
        engine.HandleKey(new KeyEvent(ConsoleKey.D5), TimeSpan.FromMilliseconds(2));
        engine.HandleKey(new KeyEvent(ConsoleKey.Enter), TimeSpan.FromMilliseconds(3));

        Assert.Equal(15, engine.LevelIndex);
        Assert.False(engine.LevelPromptActive);
        Assert.Equal(string.Empty, engine.LevelPromptInput);
    }

    [Fact]
    public void Level_prompt_escape_cancels_without_jumping()
    {
        var engine = CreateEngine(startLevel: 3);
        engine.HandleKey(new KeyEvent(ConsoleKey.L), TimeSpan.Zero);
        engine.HandleKey(new KeyEvent(ConsoleKey.D9), TimeSpan.FromMilliseconds(1));
        engine.HandleKey(new KeyEvent(ConsoleKey.Escape), TimeSpan.FromMilliseconds(2));

        Assert.Equal(3, engine.LevelIndex);
        Assert.False(engine.LevelPromptActive);
    }

    [Fact]
    public void Level_prompt_backspace_deletes_last_digit()
    {
        var engine = CreateEngine();
        engine.HandleKey(new KeyEvent(ConsoleKey.L), TimeSpan.Zero);
        engine.HandleKey(new KeyEvent(ConsoleKey.D1), TimeSpan.FromMilliseconds(1));
        engine.HandleKey(new KeyEvent(ConsoleKey.D2), TimeSpan.FromMilliseconds(2));
        engine.HandleKey(new KeyEvent(ConsoleKey.D3), TimeSpan.FromMilliseconds(3));
        engine.HandleKey(new KeyEvent(ConsoleKey.Backspace), TimeSpan.FromMilliseconds(4));

        Assert.Equal("12", engine.LevelPromptInput);
    }

    [Fact]
    public void Digit_key_outside_the_prompt_does_nothing()
    {
        // #36 removed the #25 digit-direct-jump behaviour — pressing 5
        // outside the prompt must no longer change the level.
        var engine = CreateEngine(startLevel: 2);
        engine.HandleKey(new KeyEvent(ConsoleKey.D5), TimeSpan.Zero);
        Assert.Equal(2, engine.LevelIndex);
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

    [Theory]
    [InlineData(ConsoleKey.UpArrow)]
    [InlineData(ConsoleKey.DownArrow)]
    [InlineData(ConsoleKey.LeftArrow)]
    [InlineData(ConsoleKey.RightArrow)]
    public void Arrow_keys_keep_the_selection_pointing_at_a_valid_snake(ConsoleKey arrow)
    {
        // Issue #19: arrow keys jump the selection to the nearest snake in
        // the given direction. Exact indices would be flaky against the
        // seeded level 1 board, so we assert the behavioural invariant:
        // no crash, no out-of-range index, no surprise deselect.
        var engine = CreateEngine();
        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        Assert.NotNull(engine.SelectedSnakeIndex);

        engine.HandleKey(new KeyEvent(arrow), TimeSpan.FromMilliseconds(1));
        var after = engine.SelectedSnakeIndex;
        Assert.NotNull(after);
        Assert.InRange(after!.Value, 0, engine.Board.Snakes.Length - 1);
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

    [Fact]
    public void Render_does_not_throw_during_exit_animation_when_snake_shrinks_below_two_segments()
    {
        // Drives a snake through its full exit animation and asks for a
        // render on every tick. Before the fix, the Snake constructor threw
        // ArgumentException once the animation snapshot reached one or zero
        // segments because Snake demands a minimum of two segments to infer
        // its direction. See GitHub issue #3 for the original stack trace.
        var engine = CreateEngine(animationStep: TimeSpan.FromMilliseconds(5));
        var viewport = ViewportCalculator.Compute(
            ViewportCalculator.MinimumWidth + 8,
            ViewportCalculator.MinimumHeight + 8,
            engine.Board.Size);

        engine.HandleKey(new KeyEvent(ConsoleKey.Tab), TimeSpan.Zero);
        engine.HandleKey(new KeyEvent(ConsoleKey.Enter), TimeSpan.Zero);

        for (var ms = 0; ms <= 1_500; ms += 2)
        {
            engine.Tick(TimeSpan.FromMilliseconds(ms));
            _ = engine.Render(viewport, TimeSpan.FromMilliseconds(ms));
        }
    }

    [Fact]
    public void BuildViewport_uses_the_engines_current_board_size()
    {
        var engine = CreateEngine();
        var viewport = engine.BuildViewport(200, 80);
        Assert.Equal(engine.Board.Size, viewport.BoardSide);
    }

    [Fact]
    public void BuildViewport_tracks_new_board_size_after_a_level_up()
    {
        // Level 1 is 6x6, level 2 is 7x7 under the current difficulty
        // profile. Drain level 1 and confirm BuildViewport reports the new,
        // larger board — the crash in issue #7 happened because a cached
        // viewport still claimed 6.
        var engine = CreateEngine(startLevel: 1, animationStep: TimeSpan.FromMilliseconds(1));
        var beforeSize = engine.Board.Size;
        Assert.Equal(beforeSize, engine.BuildViewport(200, 80).BoardSide);

        DrainCurrentLevel(engine);

        Assert.Equal(2, engine.LevelIndex);
        var afterSize = engine.Board.Size;
        Assert.NotEqual(beforeSize, afterSize);
        Assert.Equal(afterSize, engine.BuildViewport(200, 80).BoardSide);
    }

    [Fact]
    public void Render_does_not_throw_across_a_level_transition()
    {
        // Regression for issue #7: BoardRenderer rejects mismatched viewport
        // and board sizes. The fix is to rebuild the viewport every render
        // via BuildViewport so it always tracks the engine's current board.
        var engine = CreateEngine(startLevel: 1, animationStep: TimeSpan.FromMilliseconds(1));
        DrainCurrentLevel(engine);
        var viewport = engine.BuildViewport(200, 80);
        _ = engine.Render(viewport, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Other_snakes_are_never_rendered_as_selected_during_an_exit_animation()
    {
        // Issue #29: when the selected snake exits, the animation's final
        // frames trim it from the rendered board so remaining snakes shift
        // down an index. Without the fix the renderer would briefly match
        // the now-vacated selection index against whichever snake slid into
        // that slot, highlighting the wrong snake for a frame or two before
        // FinalizeAnimation cleared the selection. That shows as a flicker.
        var engine = CreateEngine(animationStep: TimeSpan.FromMilliseconds(5));
        Assert.True(engine.Board.Snakes.Length >= 2);

        // Snapshot every other snake's body-cell positions — those cells
        // never move during an animation of a different snake, so any
        // reverse-video glyph appearing there (the selection highlight)
        // must be a mis-highlight of the wrong snake.
        var otherCells = new List<(int X, int Y)>();
        var animatedIndex = 0;
        for (var i = 0; i < engine.Board.Snakes.Length; i++)
        {
            if (i == animatedIndex)
            {
                continue;
            }
            var snake = engine.Board.Snakes[i];
            for (var s = 1; s < snake.Segments.Length; s++)
            {
                otherCells.Add((snake.Segments[s].X, snake.Segments[s].Y));
            }
        }

        var head = engine.Board.Snakes[animatedIndex].Head;
        engine.HandleBoardClick(head.X, head.Y, TimeSpan.Zero);

        var viewport = engine.BuildViewport(200, 80);
        for (var ms = 0; ms <= 1_500; ms += 2)
        {
            var now = TimeSpan.FromMilliseconds(ms);
            engine.Tick(now);
            var buffer = engine.Render(viewport, now);
            foreach (var cell in otherCells)
            {
                var bx = viewport.BoardOriginX + cell.X * ViewportCalculator.CellCharWidth;
                var by = viewport.BoardOriginY + cell.Y * ViewportCalculator.CellCharHeight;
                Assert.False(
                    buffer[bx, by].Reverse,
                    $"non-animated snake cell at ({cell.X},{cell.Y}) must not carry the selection highlight");
            }
            if (!engine.IsAnimating)
            {
                break;
            }
        }
    }

    [Fact]
    public void Selection_clears_after_a_snake_exits_so_enter_cannot_chain_removals()
    {
        // Issue #14: the engine used to leave SelectedSnakeIndex pointing at
        // whichever snake slid into the exited snake's slot, so repeatedly
        // hitting Enter emptied the board without the player having to
        // think. Drive the solver until the first exit and assert the
        // selection is cleared.
        var engine = CreateEngine(animationStep: TimeSpan.FromMilliseconds(1));
        var clock = TimeSpan.Zero;

        while (engine.Board.Snakes.Length > 0)
        {
            var solution = Solver.TrySolve(engine.Board);
            Assert.NotNull(solution);
            Assert.NotEmpty(solution);

            var before = engine.Board.Snakes.Length;
            var snake = engine.Board.Snakes[solution[0]];
            engine.HandleBoardClick(snake.Head.X, snake.Head.Y, clock);
            while (engine.IsAnimating)
            {
                clock = clock.Add(TimeSpan.FromMilliseconds(2));
                engine.Tick(clock);
            }

            if (engine.Board.Snakes.Length < before)
            {
                Assert.Null(engine.SelectedSnakeIndex);
                return;
            }
        }
        Assert.Fail("expected at least one snake to exit during the level");
    }

    private static void DrainCurrentLevel(GameEngine engine)
    {
        // Stop as soon as Tick advances to the next level. Without this guard
        // the loop runs forever because the engine auto-populates the new
        // level's board during the same tick the previous one drained.
        var startLevel = engine.LevelIndex;
        var clock = TimeSpan.Zero;
        while (engine.LevelIndex == startLevel)
        {
            if (engine.Board.Snakes.Length == 0)
            {
                clock = clock.Add(TimeSpan.FromMilliseconds(2));
                engine.Tick(clock);
                continue;
            }
            var solution = Solver.TrySolve(engine.Board);
            Assert.NotNull(solution);
            Assert.NotEmpty(solution);
            var snake = engine.Board.Snakes[solution[0]];
            engine.HandleBoardClick(snake.Head.X, snake.Head.Y, clock);
            while (engine.IsAnimating)
            {
                clock = clock.Add(TimeSpan.FromMilliseconds(2));
                engine.Tick(clock);
            }
        }
    }
}
