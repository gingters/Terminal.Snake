# terminal-snake

A polished terminal-UI puzzle in .NET 10. A tangled tangle of colorful snakes sits in a square grid; each snake has a single, head-determined exit on the border. Slide them out one by one without deadlocking.

## Controls

| Input                | Action                                                   |
| -------------------- | -------------------------------------------------------- |
| Left-click snake     | Release that snake through its exit                      |
| `Tab` / `Shift+Tab`  | Cycle selection between snakes                           |
| `Enter` / `Space`    | Release the selected snake                               |
| `R`                  | Restart current level                                    |
| `Q` / `Esc`          | Quit                                                     |

If no input is received for 30 seconds the game auto-plays the current board in demo mode. Any input hands control back immediately.

## Snake mechanics

A snake's movement direction is derived from its head and the segment directly behind it:

| Head is ... of segment[1] | Snake exits ... |
| ------------------------- | --------------- |
| above                     | upwards         |
| below                     | downwards       |
| left of                   | leftwards       |
| right of                  | rightwards      |

The exit is the single border cell in front of the head, along that direction. Snakes advance one cell at a time; the body follows its own trail. They may stop part-way when blocked — later moves can free them again.

## Build

```
dotnet build
dotnet run --project src/TerminalSnake.App
```

Terminal needs to be at least 40×15. SGR-1006 mouse reporting is required for click input; iTerm2, macOS Terminal, Windows Terminal, kitty, and Alacritty all support it by default.

## Tests & Quality Gate

```
./scripts/quality-gate.sh            # Linux / macOS
pwsh ./scripts/quality-gate.ps1      # Windows
```

The quality gate runs tests with coverage, produces an HTML report under `artifacts/report/`, and fails the build when any of:

- cyclomatic complexity > 10 (per method)
- CRAP score > 15 (per method, computed as complexity²·(1−cov)² + complexity)
- a source class has 0 % line coverage
- overall line coverage < 85 % or branch coverage < 75 %

Per-method complexity comes straight from the Cobertura XML that coverlet emits; the gate walks every `coverage.cobertura.xml` under `artifacts/coverage/` and reports every failing method.

## Architecture at a glance

```
Domain        pure value types (Cell, Direction, Snake, Board)
Movement      MoveEngine: advance a snake as far as possible
Generation    BoardGenerator (seeded) + Solver (BFS) + FixedLevels (10 curated seeds)
Rendering     ViewportCalculator, FrameBuffer, BoardRenderer, BoardView (Spectre),
              AnimationScheduler, Theme
Input         InputDecoder (SGR-1006 mouse + CSI arrow/Tab), BufferedInputParser,
              TerminalMode
Game          GameEngine (player + demo), LevelManager, IdleWatcher
```

The top layer (`Program.cs`) wires Console + Spectre Live + an stdin pump; it is excluded from coverage because its only job is I/O plumbing around the tested `GameEngine`.

## License

MIT © 2026 Sebastian Gingter — see [LICENSE](LICENSE).
