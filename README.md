# terminal-snake

A polished terminal-UI puzzle in .NET 10. A tangled tangle of colorful snakes sits in a square grid; each snake has a single, head-determined exit on the border. Slide them out one by one without deadlocking.

> Status: Work in progress — see the [plan](#roadmap) for phase-by-phase scope.

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

- cyclomatic complexity > 10
- CRAP score > 15
- a source file has 0 % line coverage
- line coverage < 85 % or branch coverage < 75 %

## Roadmap

Implementation is phased in small conventional-commit steps; see `/Users/sebastian/.claude/plans/in-net-nach-aktuellsten-radiant-cerf.md` for the full plan (German).

## License

MIT © 2026 Sebastian Gingter — see [LICENSE](LICENSE).
