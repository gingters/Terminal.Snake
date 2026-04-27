using System.Diagnostics;
using TerminalSnake.Game;

namespace TerminalSnake.Tests.Game;

public sealed class ShutdownSequenceTests
{
    [Fact]
    public void Disable_runs_after_pump_task_completes()
    {
        // Models the real shutdown: a pump task that only finishes
        // once cancellation has been signalled, and a disable action
        // that must NOT execute while the pump is still running.
        var pumpTcs = new TaskCompletionSource();
        var disableInvokedAt = (long?)null;
        var pumpCompletedAt = (long?)null;
        using var cts = new CancellationTokenSource();

        cts.Token.Register(() =>
        {
            // Simulate the pump waking from its blocking read shortly
            // after the cancellation token fires.
#pragma warning disable xUnit1051 // shutdown is synchronous; test cancellation token irrelevant here.
            Task.Run(async () =>
            {
                await Task.Delay(20).ConfigureAwait(false);
                pumpCompletedAt = Stopwatch.GetTimestamp();
                pumpTcs.SetResult();
            });
#pragma warning restore xUnit1051
        });

        ShutdownSequence.Run(
            cancel: cts,
            pumpTask: pumpTcs.Task,
            pumpJoinTimeout: TimeSpan.FromSeconds(2),
            disable: () => disableInvokedAt = Stopwatch.GetTimestamp());

        Assert.True(pumpCompletedAt.HasValue, "Pump should have completed during shutdown");
        Assert.True(disableInvokedAt.HasValue, "Disable should have been invoked");
        Assert.True(
            disableInvokedAt!.Value >= pumpCompletedAt!.Value,
            "Disable must run only after the pump task has completed");
    }

    [Fact]
    public void Cancellation_is_signalled_before_waiting_on_pump()
    {
        var pumpStarted = new ManualResetEventSlim(false);
        var cancelObservedBeforeJoin = false;
        using var cts = new CancellationTokenSource();

#pragma warning disable xUnit1051 // background pump simulation; test cancellation token irrelevant.
        var pumpTask = Task.Run(() =>
        {
            pumpStarted.Set();
            // Spin until cancellation, then exit. If the sequence does
            // NOT cancel before waiting, this task never completes and
            // the join times out — test fails.
            while (!cts.IsCancellationRequested)
            {
                Thread.Sleep(5);
            }
            cancelObservedBeforeJoin = true;
        });
#pragma warning restore xUnit1051

        Assert.True(pumpStarted.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        ShutdownSequence.Run(
            cancel: cts,
            pumpTask: pumpTask,
            pumpJoinTimeout: TimeSpan.FromSeconds(2),
            disable: () => { });

        Assert.True(pumpTask.IsCompleted, "Pump task must have completed before disable");
        Assert.True(cancelObservedBeforeJoin, "Pump should have observed cancellation");
    }

    [Fact]
    public void Disable_runs_even_when_pump_does_not_exit_within_timeout()
    {
        // Under VTIME=0 the pump can be stuck in a blocking read with
        // no way to wake it. The shutdown sequence must still restore
        // the terminal — leaving the tty in raw mode is worse than
        // leaking a thread for a few ms.
        using var cts = new CancellationTokenSource();
        var stuckPump = new TaskCompletionSource().Task;
        var disableInvoked = false;

        ShutdownSequence.Run(
            cancel: cts,
            pumpTask: stuckPump,
            pumpJoinTimeout: TimeSpan.FromMilliseconds(50),
            disable: () => disableInvoked = true);

        Assert.True(disableInvoked, "Disable must run even when the pump is stuck");
    }

    [Fact]
    public void Disable_runs_after_pump_join_times_out_not_before()
    {
        using var cts = new CancellationTokenSource();
        var stuckPump = new TaskCompletionSource().Task;
        var startedAt = Stopwatch.GetTimestamp();
        long disabledAt = 0;
        var timeout = TimeSpan.FromMilliseconds(120);

        ShutdownSequence.Run(
            cancel: cts,
            pumpTask: stuckPump,
            pumpJoinTimeout: timeout,
            disable: () => disabledAt = Stopwatch.GetTimestamp());

        var elapsed = Stopwatch.GetElapsedTime(startedAt, disabledAt);
        Assert.True(
            elapsed >= timeout - TimeSpan.FromMilliseconds(20),
            $"Disable should wait for the join timeout before running; elapsed {elapsed}");
    }

    [Fact]
    public void Suppresses_pump_task_exception_so_disable_still_runs()
    {
        // The pump catches IO failures itself, but defence in depth:
        // an exception bubbling out of the pump task must not stop
        // the terminal from being restored.
        using var cts = new CancellationTokenSource();
        var faulted = Task.FromException(new InvalidOperationException("pump blew up"));
        var disableInvoked = false;

        ShutdownSequence.Run(
            cancel: cts,
            pumpTask: faulted,
            pumpJoinTimeout: TimeSpan.FromMilliseconds(50),
            disable: () => disableInvoked = true);

        Assert.True(disableInvoked);
    }

    [Fact]
    public void Throws_when_arguments_are_null()
    {
        using var cts = new CancellationTokenSource();
        var task = Task.CompletedTask;

        Assert.Throws<ArgumentNullException>(() =>
            ShutdownSequence.Run(null!, task, TimeSpan.FromMilliseconds(10), () => { }));
        Assert.Throws<ArgumentNullException>(() =>
            ShutdownSequence.Run(cts, null!, TimeSpan.FromMilliseconds(10), () => { }));
        Assert.Throws<ArgumentNullException>(() =>
            ShutdownSequence.Run(cts, task, TimeSpan.FromMilliseconds(10), null!));
    }
}
