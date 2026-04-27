using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Channels;
using Spectre.Console;
using TerminalSnake.Game;
using TerminalSnake.Generation;
using TerminalSnake.Input;
using TerminalSnake.Rendering;

namespace TerminalSnake;

// Composition root. Exercised by manual smoke tests only; the interesting
// logic lives in GameEngine, which is covered by unit tests. Everything
// here is console I/O and thread plumbing, deliberately excluded from
// coverage so gating does not regress on changes to terminal wiring.
[ExcludeFromCodeCoverage]
internal static class Program
{
    private static int Main(string[] args)
    {
        // Pin the console to UTF-8 before any write so the box-drawing
        // glyphs (═ ║ ╔ ╗ ╚ ╝ ► ◄ ▲ ▼) survive the journey. Without
        // this, Windows console hosts default to the OEM code page
        // (CP437/CP850 depending on locale) and our multi-byte UTF-8
        // sequences land as garbage that shifts subsequent columns.
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            return Run();
        }
        catch (TerminalTooSmallException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static int Run()
    {
        // Probe the terminal up front so the engine can size its boards
        // to the screen — "game area too small" (issue #36) was the
        // generator hard-capping at 16 regardless of how much real estate
        // the terminal actually offered. MaxBoardSide falls back to the
        // generator's cap when the terminal is small, so the minimum
        // tutorial layout still fits.
        var maxBoardSide = Math.Max(
            BoardGenerator.MinBoardSize,
            ViewportCalculator.MaxBoardSide(Console.WindowWidth, Console.WindowHeight));
        var engine = new GameEngine(
            hudStrings: HudLocalization.ForCurrentEnvironment(),
            maxBoardSide: maxBoardSide);
        // Probe once so we fail fast if the terminal is too small for the
        // starting board; the live loop rebuilds the viewport on every
        // tick so later level transitions pick up a new board size.
        _ = engine.BuildViewport(Console.WindowWidth, Console.WindowHeight);

        using var terminalMode = new TerminalMode();
        terminalMode.Enable(Console.Out);
        // Disable is invoked by RunLoop via ShutdownSequence so it runs
        // AFTER the stdin pump has stopped (issue #47). The `using` above
        // is a safety net for the exception path: if RunLoop throws
        // before reaching the shutdown sequence, Dispose still restores
        // the terminal — it just lacks the synchronization guarantee
        // because there's nothing left to synchronize with at that
        // point.
        RunLoop(engine, terminalMode);
        return 0;
    }

    private static void RunLoop(GameEngine engine, TerminalMode terminalMode)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var events = Channel.CreateUnbounded<InputEvent>();
        var reader = Task.Run(() => PumpStdin(events.Writer, cts.Token));
        try
        {
            DriveLiveLoop(engine, events, cts);
        }
        finally
        {
            events.Writer.TryComplete();
            // Single ordered shutdown: cancel → wait for pump → restore
            // terminal. Closes the tcsetattr/stdin.Read race from #47.
            // The 250 ms timeout is the upper bound for the pump to
            // observe cancellation under VTIME=1 (#50); under VTIME=0
            // a parked read may not wake at all, in which case we
            // still proceed to Disable rather than hang the user's
            // shell — see ShutdownSequence for the trade-off.
            ShutdownSequence.Run(
                cancel: cts,
                pumpTask: reader,
                pumpJoinTimeout: TimeSpan.FromMilliseconds(250),
                disable: () => terminalMode.Disable(Console.Out));
        }
    }

    private static void DriveLiveLoop(
        GameEngine engine,
        Channel<InputEvent> events,
        CancellationTokenSource cts)
    {
        var stopwatch = Stopwatch.StartNew();
        var viewport = engine.BuildViewport(Console.WindowWidth, Console.WindowHeight);
        var initialBuffer = engine.Render(viewport, stopwatch.Elapsed);
        var view = new BoardView(initialBuffer);

        // AutoClear(false): Spectre diffs cell-by-cell between refreshes instead of
        // wiping the region, so a one-off input (Tab, selection change, ...) does
        // not repaint the whole frame top-to-bottom and the board stays still.
        // Overflow(Crop): never scroll when the renderable matches the terminal
        // height; scrolling would nudge the frame up/down visibly on each tick.
        AnsiConsole.Live(view)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Crop)
            .Cropping(VerticalOverflowCropping.Bottom)
            .Start(ctx =>
            {
                while (!cts.IsCancellationRequested)
                {
                    // Input translation (click → board cell) needs the viewport
                    // matching what the player currently sees, i.e. the board
                    // BEFORE Tick. Render needs the viewport matching the
                    // board AFTER Tick, which may have transitioned to the
                    // next level (different Board.Size). Build a fresh
                    // viewport for each phase.
                    var inputViewport = engine.BuildViewport(Console.WindowWidth, Console.WindowHeight);
                    DrainEvents(events.Reader, engine, inputViewport, stopwatch.Elapsed, cts);
                    engine.Tick(stopwatch.Elapsed);
                    var renderViewport = engine.BuildViewport(Console.WindowWidth, Console.WindowHeight);
                    view.Update(engine.Render(renderViewport, stopwatch.Elapsed));
                    ctx.Refresh();
                    Thread.Sleep(16);
                }
            });
    }

    private static void DrainEvents(
        ChannelReader<InputEvent> reader,
        GameEngine engine,
        Viewport viewport,
        TimeSpan now,
        CancellationTokenSource cts)
    {
        while (reader.TryRead(out var evt))
        {
            HandleEvent(evt, engine, viewport, now, cts);
        }
    }

    private static void HandleEvent(
        InputEvent evt, GameEngine engine, Viewport viewport, TimeSpan now, CancellationTokenSource cts)
    {
        switch (evt)
        {
            case KeyEvent key when IsQuitKey(key, engine):
                cts.Cancel();
                return;
            case KeyEvent key:
                engine.HandleKey(key, now);
                return;
            case MouseClickEvent click:
                var (boardX, boardY) = TerminalToBoard(click, viewport);
                if (boardX >= 0 && boardY >= 0 && boardX < engine.Board.Size && boardY < engine.Board.Size)
                {
                    engine.HandleBoardClick(boardX, boardY, now);
                }
                return;
        }
    }

    // Esc also cancels the level-jump prompt (#36), so while the prompt
    // is open we hand the key to the engine instead of quitting the app.
    private static bool IsQuitKey(KeyEvent key, GameEngine engine) =>
        key.Key == ConsoleKey.Q
        || (key.Key == ConsoleKey.Escape && !engine.LevelPromptActive);

    private static (int BoardX, int BoardY) TerminalToBoard(MouseClickEvent click, Viewport viewport) =>
        ((click.Column - viewport.BoardOriginX) / ViewportCalculator.CellCharWidth,
         (click.Row - viewport.BoardOriginY) / ViewportCalculator.CellCharHeight);

    private static void PumpStdin(ChannelWriter<InputEvent> writer, CancellationToken ct)
    {
        var parser = new BufferedInputParser();
        var stdin = OperatingSystem.IsWindows()
            ? Console.OpenStandardInput()
            : PosixTerminal.OpenTtyReadStream();
        var buffer = new byte[128];
        while (!ct.IsCancellationRequested)
        {
            int read;
            try
            {
                read = stdin.Read(buffer, 0, buffer.Length);
            }
            catch
            {
                break;
            }
            if (read == 0)
            {
                Thread.Sleep(10);
                continue;
            }
            var events = parser.Feed(buffer.AsSpan(0, read));
            foreach (var evt in events)
            {
                if (!writer.TryWrite(evt))
                {
                    return;
                }
            }
        }
    }
}
