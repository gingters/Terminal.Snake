using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace TerminalSnake;

// Composition root is intentionally excluded from coverage: it is a thin wiring layer
// that is exercised via manual smoke tests (see scripts/quality-gate.{sh,ps1}).
[ExcludeFromCodeCoverage]
internal static class Program
{
    private static int Main(string[] args)
    {
        AnsiConsole.MarkupLine("[green]TerminalSnake[/] — scaffolded. Gameplay wiring comes in later phases.");
        return 0;
    }
}
