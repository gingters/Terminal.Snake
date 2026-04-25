# Containerized test environments

Disposable Docker images for kicking the tires on the published Linux Native-AoT
binaries without polluting a host system. Geared at running on an Apple Silicon
Mac via Docker Desktop:

- `linux-arm64` runs natively
- `linux-x64`   runs through Rosetta / QEMU emulation

Each image is a tiny Ubuntu 24.04 with the bare runtime libs the AoT binary
needs, plus the `terminal-snake` binary on `PATH`. The container's default
command is `bash`, so you log in, run `terminal-snake`, exit with Ctrl+D.

## Layout

```
test-environments/
├── README.md
├── fetch-release.sh         # download archives from a GitHub release
├── fetch-ci-artifacts.sh    # download archives from a workflow run (needs gh)
├── run.sh                   # docker build + interactive run
├── linux-x64/Dockerfile
├── linux-arm64/Dockerfile
└── bin/                     # populated by the fetch scripts (gitignored)
    ├── linux-x64/
    └── linux-arm64/
```

## Quick start

```bash
cd test-environments

# 1. Pull the latest release archives into bin/<rid>/
./fetch-release.sh

# 2. Build the image and drop into a shell
./run.sh arm64        # or: ./run.sh x64

# 3. Inside the container
terminal-snake
```

## Fetching binaries

### From a GitHub release (default)

```bash
./fetch-release.sh                # latest published release
./fetch-release.sh v0.3.1         # specific tag
GH_TOKEN=ghp_xxx ./fetch-release.sh   # avoid anonymous rate limits
```

### From a CI workflow run

The `Release Artifacts` workflow uploads the same archives as workflow
artifacts on dry-run pushes to `feature/release`. Useful for testing a build
before tagging a release.

```bash
./fetch-ci-artifacts.sh           # latest successful run
./fetch-ci-artifacts.sh 1234567   # specific run id
```

Requires the [`gh` CLI](https://cli.github.com), authenticated with
`gh auth login` (artifacts are not anonymously downloadable).

## Running

```bash
./run.sh                # defaults to arm64
./run.sh x64            # linux/amd64 (Rosetta on Apple Silicon)
./run.sh arm64 --build  # force rebuild without cache
./run.sh x64 -- terminal-snake   # run the app directly instead of bash
```

The script mirrors the architecture you asked for to both `docker build
--platform` and `docker run --platform`, and uses `--rm -it` so the
container is gone on exit.

Inside the container:

- `terminal-snake` is on `PATH`
- `TERM=xterm-256color`, `LANG=C.UTF-8` are pre-set
- Working directory is `/opt/terminal-snake`

Mouse input (SGR-1006) and 256-color rendering work out of the box from
iTerm2, macOS Terminal, and most modern terminals — Docker just forwards the
host TTY.

## Notes

- `bin/` is gitignored — re-fetch any time you want fresh binaries.
- The Dockerfiles `COPY` from `bin/<rid>/` at build time, so each
  `./run.sh` rebuild picks up whatever is currently staged there.
- If a release pre-dates the `linux-arm64` / `linux-x64` archives or uses a
  different naming scheme, override the binary by manually placing
  `terminal-snake` (and any companion files) into `bin/<rid>/`.
