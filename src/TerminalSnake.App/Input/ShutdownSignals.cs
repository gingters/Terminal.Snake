using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace TerminalSnake.Input;

// #49 — Without these registrations only Ctrl+C runs Program.Run's finally
// block. SIGTERM (`kill <pid>`), SIGHUP (terminal close), and SIGQuit
// (Ctrl+\) bypass it and the tty stays in raw mode, forcing the user to
// type `stty sane` blind. Hooking the same cancellation source the
// Console.CancelKeyPress handler drives gives the existing shutdown path
// a chance to restore the terminal on every recoverable signal.
//
// SIGKILL is intentionally omitted — the kernel does not deliver it to the
// process so no handler can run.
public static class ShutdownSignals
{
    /// <summary>
    /// The set of POSIX signals that should drive the shared shutdown
    /// path. Exposed so tests can pin the intended set without exercising
    /// real signal delivery.
    /// </summary>
    public static ImmutableArray<PosixSignal> Signals { get; } = ImmutableArray.Create(
        PosixSignal.SIGINT,
        PosixSignal.SIGTERM,
        PosixSignal.SIGHUP,
        PosixSignal.SIGQUIT);

    /// <summary>
    /// Registers a handler for every signal in <see cref="Signals"/>.
    /// Each handler cancels <paramref name="cts"/> and marks the signal
    /// as handled so the runtime skips its default termination, giving
    /// the cancellation token consumer time to tear down the terminal.
    /// </summary>
    /// <param name="cts">Cancellation source driven by the handlers.</param>
    /// <param name="factory">
    /// Factory that returns an <see cref="IDisposable"/> for a
    /// (signal, handler) pair. Defaults to
    /// <see cref="PosixSignalRegistration.Create"/> in production; tests
    /// inject a fake to verify wiring without raising real signals.
    /// </param>
    public static IDisposable Register(
        CancellationTokenSource cts,
        Func<PosixSignal, Action<PosixSignalContext>, IDisposable>? factory = null)
    {
        ArgumentNullException.ThrowIfNull(cts);
        var create = factory ?? DefaultFactory;
        var registrations = new List<IDisposable>(Signals.Length);
        foreach (var signal in Signals)
        {
            registrations.Add(create(signal, ctx =>
            {
                ctx.Cancel = true;
                cts.Cancel();
            }));
        }
        return new CompositeDisposable(registrations);
    }

    private static IDisposable DefaultFactory(PosixSignal signal, Action<PosixSignalContext> handler)
        => PosixSignalRegistration.Create(signal, handler);

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IReadOnlyList<IDisposable> _items;
        private bool _disposed;

        public CompositeDisposable(IReadOnlyList<IDisposable> items)
        {
            _items = items;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            foreach (var item in _items)
            {
                item.Dispose();
            }
        }
    }
}
