namespace TerminalSnake.Game;

// Orchestrates the four-step shutdown of the live game loop in a
// deterministic order so the tty restore (`tcsetattr`) never races
// the background stdin pump's blocking `Read`. Issue #47.
//
// Order is load-bearing:
//   1. Cancel — flips the token so the pump's loop predicate fails
//      on its next iteration.
//   2. Join   — waits up to `pumpJoinTimeout` for the pump task to
//      observe cancellation and return. A bounded wait, not an
//      indefinite one, because under VTIME=0 the pump can be
//      parked inside `read(2)` with no way to wake; we still owe
//      the user a restored terminal in that case (see comment in
//      `Run`).
//   3. Disable — runs the terminal-restore action (xterm sequences
//      + `tcsetattr`). This is the call that previously raced the
//      pump and the whole reason for this seam.
//
// Kept as a static seam (not a class) because it owns no state — it
// is purely an ordering policy. Pulled out of `Program.RunLoop` so
// it can be unit-tested without a real terminal.
public static class ShutdownSequence
{
    public static void Run(
        CancellationTokenSource cancel,
        Task pumpTask,
        TimeSpan pumpJoinTimeout,
        Action disable)
    {
        ArgumentNullException.ThrowIfNull(cancel);
        ArgumentNullException.ThrowIfNull(pumpTask);
        ArgumentNullException.ThrowIfNull(disable);

        // Step 1 — signal the pump. Cancel BEFORE joining: otherwise
        // the wait would deadlock (pump never wakes) for the full
        // timeout on every shutdown.
        try
        {
            cancel.Cancel();
        }
        catch (AggregateException)
        {
            // Token registrations may throw; we still need to keep
            // walking through Disable. Swallow and continue.
        }

        // Step 2 — give the pump a deterministic chance to exit.
        // `Task.Wait(timeout)` returns false on timeout (no throw),
        // and rethrows AggregateException for a faulted task — which
        // we eat: the pump catches its own IO failures, but if one
        // somehow escapes, leaving the terminal in raw mode is worse
        // than dropping the exception.
        //
        // Limitation: with VTIME=0 (current main as of #47) the pump
        // can be parked inside a blocking `read(2)` and won't observe
        // the cancellation token until bytes arrive. The bounded join
        // ensures we still proceed to Disable; #50 raises VTIME so
        // the read self-wakes and the join completes promptly. This
        // sequence composes with either VTIME setting unchanged.
        try
        {
            pumpTask.Wait(pumpJoinTimeout);
        }
        catch (AggregateException)
        {
            // Pump faulted — terminal restore still has to run.
        }

        // Step 3 — restore the terminal. Now safe: the pump has
        // either exited or been declared a lost cause, so no
        // concurrent `stdin.Read` can collide with `tcsetattr`.
        disable();
    }
}
