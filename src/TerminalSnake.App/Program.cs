using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Spectre.Console;
using TerminalSnake.Game;
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
        var engine = new GameEngine();
        // Probe once up-front so we fail fast if the terminal is too small
        // for the starting board; the live loop rebuilds the viewport on
        // every tick so later level transitions pick up a new board size.
        _ = engine.BuildViewport(Console.WindowWidth, Console.WindowHeight);

        using var terminalMode = new TerminalMode();
        terminalMode.Enable(Console.Out);
        try
        {
            RunLoop(engine);
        }
        finally
        {
            terminalMode.Disable(Console.Out);
        }
        return 0;
    }

    private static void RunLoop(GameEngine engine)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var events = Channel.CreateUnbounded<InputEvent>();
        var reader = Task.Run(() => PumpStdin(events.Writer, cts.Token));
        var stopwatch = Stopwatch.StartNew();
        var viewport = engine.BuildViewport(Console.WindowWidth, Console.WindowHeight);
        var initialBuffer = engine.Render(viewport, stopwatch.Elapsed);
        var view = new BoardView(initialBuffer);

        AnsiConsole.Live(view)
            .AutoClear(true)
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

        cts.Cancel();
        events.Writer.TryComplete();
        try
        {
            reader.Wait(TimeSpan.FromMilliseconds(200));
        }
        catch
        {
            // Reader task may throw during shutdown; suppress.
        }
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
            case KeyEvent key when IsQuitKey(key):
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

    private static bool IsQuitKey(KeyEvent key) =>
        key.Key == ConsoleKey.Q || key.Key == ConsoleKey.Escape;

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
