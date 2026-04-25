#!/usr/bin/env bash
# Build the test image for the chosen RID (using whatever sits in bin/<rid>/)
# and drop into an interactive shell. Type `terminal-snake` to launch the app.
#
# Usage:
#   ./run.sh                # defaults to arm64 (native on Apple Silicon)
#   ./run.sh x64            # linux/amd64 (Rosetta/QEMU on Apple Silicon)
#   ./run.sh arm64
#   ./run.sh x64 --build    # force rebuild (no cache)
#   ./run.sh x64 -- terminal-snake   # run a command instead of bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"
cd "$SCRIPT_DIR"

ARCH="${1:-arm64}"; shift || true

case "$ARCH" in
    x64|amd64|linux-x64)        RID=linux-x64;  PLATFORM=linux/amd64 ;;
    arm64|aarch64|linux-arm64)  RID=linux-arm64; PLATFORM=linux/arm64 ;;
    -h|--help)
        sed -n '2,11p' "$0"; exit 0 ;;
    *)
        echo "Unknown arch: $ARCH (expected x64|arm64)" >&2
        exit 1
        ;;
esac

BUILD_ARGS=()
CMD=()
while (( $# )); do
    case "$1" in
        --build|--no-cache) BUILD_ARGS+=(--no-cache) ;;
        --) shift; CMD=("$@"); break ;;
        *) echo "Unknown option: $1" >&2; exit 1 ;;
    esac
    shift
done

if [[ ! -f "bin/$RID/terminal-snake" ]]; then
    cat >&2 <<EOF
No binary found at bin/$RID/terminal-snake.

Fetch one first:
  ./fetch-release.sh           # latest published release
  ./fetch-ci-artifacts.sh      # latest successful CI run (needs gh CLI)
EOF
    exit 1
fi

IMAGE="terminal-snake-test:$RID"
echo "Building $IMAGE for $PLATFORM..."
docker build \
    "${BUILD_ARGS[@]}" \
    --platform "$PLATFORM" \
    -f "$RID/Dockerfile" \
    -t "$IMAGE" \
    .

echo "Starting container ($IMAGE). Run 'terminal-snake' inside, exit with Ctrl+D."
exec docker run \
    --rm -it \
    --platform "$PLATFORM" \
    "$IMAGE" \
    "${CMD[@]}"
