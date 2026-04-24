using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using TerminalSnake.Generation;

namespace TerminalSnake.Benchmarks;

// Measures how long a single level generation takes. Runs with a single
// outer iteration (plus BenchmarkDotNet's overhead/warmup) because higher
// levels can take seconds — running dozens of iterations per level would
// turn the bench into an all-night job. Uses the in-process toolchain to
// avoid BenchmarkDotNet's default "walk up every ancestor directory"
// project-discovery logic, which trips on OneDrive folders the current
// user can't traverse.
[Config(typeof(Config))]
public class LevelGenerationBenchmark
{
    private const int TerminalSide = 40;
    private BoardGenerator _generator = null!;

    [Params(1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 150, 200, 500, 999)]
    public int Level { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _generator = new BoardGenerator();
    }

    [Benchmark]
    public int Generate()
    {
        var board = _generator.Generate(Level, seed: 42, maxBoardSide: TerminalSide);
        return board.Snakes.Length;
    }

    private sealed class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(1)
                .WithIterationCount(3)
                .WithInvocationCount(1)
                .WithUnrollFactor(1));
            AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
        }
    }
}
