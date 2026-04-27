using System.Runtime.InteropServices;
using TerminalSnake.Input;

namespace TerminalSnake.Tests.Input;

// #49 — Without SIGTERM/SIGHUP/SIGQUIT registrations the finally block in
// Program.Run never runs on `kill <pid>`, terminal close, or Ctrl+\, leaving
// the tty in raw mode. These tests pin the intended signal set and the
// cancellation wiring through a test-friendly seam.
public sealed class ShutdownSignalsTests
{
    [Fact]
    public void Signals_cover_sigint_sigterm_sighup_sigquit()
    {
        Assert.Contains(PosixSignal.SIGINT, ShutdownSignals.Signals);
        Assert.Contains(PosixSignal.SIGTERM, ShutdownSignals.Signals);
        Assert.Contains(PosixSignal.SIGHUP, ShutdownSignals.Signals);
        Assert.Contains(PosixSignal.SIGQUIT, ShutdownSignals.Signals);
    }

    [Fact]
    public void Register_subscribes_one_handler_per_intended_signal()
    {
        var fake = new FakeSignalRegistry();
        using var cts = new CancellationTokenSource();

        using var registration = ShutdownSignals.Register(cts, fake.Create);

        Assert.Equal(ShutdownSignals.Signals.Length, fake.Subscriptions.Count);
        foreach (var signal in ShutdownSignals.Signals)
        {
            Assert.Contains(signal, fake.Subscriptions.Select(s => s.Signal));
        }
    }

    [Fact]
    public void Triggering_any_registered_signal_cancels_the_token_source()
    {
        foreach (var signal in ShutdownSignals.Signals)
        {
            var fake = new FakeSignalRegistry();
            using var cts = new CancellationTokenSource();
            using var registration = ShutdownSignals.Register(cts, fake.Create);

            var match = fake.Subscriptions.Single(s => s.Signal == signal);
            match.Fire();

            Assert.True(cts.IsCancellationRequested,
                $"Expected {signal} handler to cancel the token source.");
        }
    }

    [Fact]
    public void Registered_handler_marks_signal_as_handled_so_runtime_skips_default_action()
    {
        // The whole point of intercepting these signals is to preempt the
        // runtime's default termination — without Cancel = true the process
        // dies before our finally block can restore the tty.
        var fake = new FakeSignalRegistry();
        using var cts = new CancellationTokenSource();
        using var registration = ShutdownSignals.Register(cts, fake.Create);

        foreach (var sub in fake.Subscriptions)
        {
            var ctx = sub.Fire();
            Assert.True(ctx.Cancel,
                $"Expected {sub.Signal} handler to set Cancel=true to suppress default.");
        }
    }

    [Fact]
    public void Disposing_register_result_disposes_every_underlying_registration()
    {
        var fake = new FakeSignalRegistry();
        using var cts = new CancellationTokenSource();

        var registration = ShutdownSignals.Register(cts, fake.Create);
        registration.Dispose();

        Assert.All(fake.Subscriptions, s => Assert.True(s.Disposed));
    }

    private sealed class FakeSignalRegistry
    {
        public List<FakeSubscription> Subscriptions { get; } = new();

        public IDisposable Create(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            var sub = new FakeSubscription(signal, handler);
            Subscriptions.Add(sub);
            return sub;
        }
    }

    private sealed class FakeSubscription : IDisposable
    {
        private readonly Action<PosixSignalContext> _handler;

        public FakeSubscription(PosixSignal signal, Action<PosixSignalContext> handler)
        {
            Signal = signal;
            _handler = handler;
        }

        public PosixSignal Signal { get; }
        public bool Disposed { get; private set; }

        public PosixSignalContext Fire()
        {
            // PosixSignalContext has no public constructor; the runtime
            // hands one to the handler. Allocate an uninitialised instance
            // so the handler can flip Cancel — that is the only field the
            // shutdown handler reads or writes.
            var ctx = (PosixSignalContext)System.Runtime.CompilerServices
                .RuntimeHelpers.GetUninitializedObject(typeof(PosixSignalContext));
            _handler(ctx);
            return ctx;
        }

        public void Dispose() => Disposed = true;
    }
}
