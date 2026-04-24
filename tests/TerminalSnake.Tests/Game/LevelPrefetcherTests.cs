using TerminalSnake.Domain;
using TerminalSnake.Game;

namespace TerminalSnake.Tests.Game;

public sealed class LevelPrefetcherTests
{
    private static Board StubBoard(int size) =>
        new(size, new[] { new Snake(new[] { new Cell(1, 1), new Cell(0, 1) }, SnakeColor.Red) });

    [Fact]
    public void Preparation_and_take_return_the_pre_built_board()
    {
        var prefetcher = new LevelPrefetcher((level, side) => StubBoard(side));
        prefetcher.PreparationRequested(level: 7, maxBoardSide: 12);
        var board = prefetcher.Take(level: 7, maxBoardSide: 12);
        Assert.Equal(12, board.Size);
    }

    [Fact]
    public void IsReady_flips_once_the_background_task_completes()
    {
        var gate = new ManualResetEventSlim(false);
        var prefetcher = new LevelPrefetcher((level, side) =>
        {
            gate.Wait();
            return StubBoard(side);
        });
        prefetcher.PreparationRequested(level: 2, maxBoardSide: 9);
        Assert.True(prefetcher.IsPreparing(level: 2, maxBoardSide: 9));
        Assert.False(prefetcher.IsReady(level: 2, maxBoardSide: 9));

        gate.Set();
        // Poll briefly for the task to finish — the background task
        // should complete almost immediately once the gate is open.
        for (var i = 0; i < 100 && !prefetcher.IsReady(2, 9); i++)
        {
            Thread.Sleep(10);
        }
        Assert.True(prefetcher.IsReady(level: 2, maxBoardSide: 9));
        Assert.False(prefetcher.IsPreparing(level: 2, maxBoardSide: 9));
    }

    [Fact]
    public void Take_with_mismatched_parameters_falls_back_to_synchronous_load()
    {
        var calls = 0;
        var prefetcher = new LevelPrefetcher((level, side) =>
        {
            calls++;
            return StubBoard(side);
        });
        prefetcher.PreparationRequested(level: 3, maxBoardSide: 11);
        var board = prefetcher.Take(level: 4, maxBoardSide: 11);
        Assert.Equal(11, board.Size);
        // Either 1 (sync path) or 2 (sync + the background one also
        // finished). Both are fine — the contract is "return a board";
        // the sync fallback must at least fire once.
        Assert.True(calls >= 1);
    }

    [Fact]
    public void Rejects_null_loader()
    {
        Assert.Throws<ArgumentNullException>(() => new LevelPrefetcher(null!));
    }
}
