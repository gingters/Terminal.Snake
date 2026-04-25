#!/usr/bin/env bash
# Download the linux-x64 / linux-arm64 release archives for terminal-snake
# and stage them under bin/<rid>/ so the Dockerfiles can COPY them in.
#
# Usage:
#   ./fetch-release.sh              # latest published release
#   ./fetch-release.sh v0.3.1       # specific tag
#   ./fetch-release.sh -- linux-x64 # only one RID (after --)
#
# Env:
#   REPO     override (default: gingters/terminal.snake)
#   GH_TOKEN optional GitHub token for private/rate-limited access

set -euo pipefail

REPO="${REPO:-gingters/terminal.snake}"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"
cd "$SCRIPT_DIR"

TAG=""
RIDS=()
while (( $# )); do
    case "$1" in
        --) shift; RIDS+=("$@"); break ;;
        -h|--help) sed -n '2,12p' "$0"; exit 0 ;;
        *) TAG="$1" ;;
    esac
    shift
done
[[ ${#RIDS[@]} -eq 0 ]] && RIDS=(linux-x64 linux-arm64)

auth_header=()
if [[ -n "${GH_TOKEN:-}" ]]; then
    auth_header=(-H "Authorization: Bearer $GH_TOKEN")
fi

if [[ -z "$TAG" || "$TAG" == "latest" ]]; then
    echo "Resolving latest release for $REPO..."
    TAG=$(curl -fsSL "${auth_header[@]}" \
        -H "Accept: application/vnd.github+json" \
        "https://api.github.com/repos/$REPO/releases/latest" \
        | grep -o '"tag_name":[[:space:]]*"[^"]*"' \
        | head -n1 \
        | sed 's/.*"\([^"]*\)"$/\1/')
fi

if [[ -z "$TAG" ]]; then
    echo "ERROR: could not determine release tag" >&2
    exit 1
fi

echo "Using release: $TAG"

for rid in "${RIDS[@]}"; do
    archive="terminal-snake-${TAG}-${rid}.tar.gz"
    url="https://github.com/$REPO/releases/download/${TAG}/${archive}"
    echo "==> $rid"
    echo "    $url"

    tmp="$(mktemp -d)"
    trap 'rm -rf "$tmp"' EXIT

    if ! curl -fL --retry 3 --retry-delay 2 "${auth_header[@]}" \
            -o "$tmp/$archive" "$url"; then
        echo "ERROR: download failed for $rid" >&2
        exit 1
    fi

    rm -rf "bin/$rid"
    mkdir -p "bin/$rid"
    tar -xzf "$tmp/$archive" -C "bin/$rid"
    rm -rf "$tmp"
    trap - EXIT

    if [[ ! -x "bin/$rid/terminal-snake" ]]; then
        chmod +x "bin/$rid/terminal-snake" 2>/dev/null || true
    fi
    echo "    -> bin/$rid/"
done

echo "Done. Build with: ./run.sh <x64|arm64>"
