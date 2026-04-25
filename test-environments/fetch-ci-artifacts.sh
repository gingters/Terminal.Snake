#!/usr/bin/env bash
# Download workflow artifacts (linux-x64 / linux-arm64 archives) from the most
# recent successful 'Release Artifacts' run, or a specific run id.
#
# Requires the GitHub CLI (`gh`) for authenticated artifact download.
#
# Usage:
#   ./fetch-ci-artifacts.sh                # latest successful run
#   ./fetch-ci-artifacts.sh 1234567890     # specific run id
#   ./fetch-ci-artifacts.sh -- linux-x64   # only one RID
#
# Env:
#   REPO      override (default: gingters/terminal.snake)
#   WORKFLOW  workflow name (default: 'Release Artifacts')

set -euo pipefail

REPO="${REPO:-gingters/terminal.snake}"
WORKFLOW="${WORKFLOW:-Release Artifacts}"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"
cd "$SCRIPT_DIR"

if ! command -v gh >/dev/null 2>&1; then
    echo "ERROR: GitHub CLI 'gh' not found. Install: https://cli.github.com" >&2
    exit 1
fi

RUN_ID=""
RIDS=()
while (( $# )); do
    case "$1" in
        --) shift; RIDS+=("$@"); break ;;
        -h|--help) sed -n '2,13p' "$0"; exit 0 ;;
        *) RUN_ID="$1" ;;
    esac
    shift
done
[[ ${#RIDS[@]} -eq 0 ]] && RIDS=(linux-x64 linux-arm64)

if [[ -z "$RUN_ID" ]]; then
    echo "Looking up latest successful '$WORKFLOW' run on $REPO..."
    RUN_ID=$(gh run list \
        --repo "$REPO" \
        --workflow "$WORKFLOW" \
        --limit 30 \
        --json databaseId,status,conclusion \
        --jq 'map(select(.status == "completed" and .conclusion == "success")) | .[0].databaseId')
fi

if [[ -z "$RUN_ID" || "$RUN_ID" == "null" ]]; then
    echo "ERROR: could not find a successful '$WORKFLOW' run" >&2
    exit 1
fi

echo "Using workflow run: $RUN_ID"

for rid in "${RIDS[@]}"; do
    artifact="terminal-snake-${rid}"
    echo "==> $rid (artifact: $artifact)"

    tmp="$(mktemp -d)"
    trap 'rm -rf "$tmp"' EXIT

    if ! gh run download "$RUN_ID" \
            --repo "$REPO" \
            --name "$artifact" \
            --dir "$tmp"; then
        echo "ERROR: failed to download artifact $artifact" >&2
        exit 1
    fi

    archive="$(find "$tmp" -maxdepth 2 -name 'terminal-snake-*-'"$rid"'.tar.gz' -print -quit)"
    if [[ -z "$archive" ]]; then
        echo "ERROR: no .tar.gz archive found in artifact $artifact" >&2
        exit 1
    fi

    rm -rf "bin/$rid"
    mkdir -p "bin/$rid"
    tar -xzf "$archive" -C "bin/$rid"
    rm -rf "$tmp"
    trap - EXIT

    if [[ ! -x "bin/$rid/terminal-snake" ]]; then
        chmod +x "bin/$rid/terminal-snake" 2>/dev/null || true
    fi
    echo "    -> bin/$rid/"
done

echo "Done. Build with: ./run.sh <x64|arm64>"
