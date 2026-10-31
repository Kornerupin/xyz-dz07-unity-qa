#!/usr/bin/env bash
# Download the official, checksum-pinned runtime into this ephemeral CI runner.
set -euo pipefail
version=0.40.13
sha=c9490e0f520be21271d969e6703f012268b1d39f8b70dd58854e5bfef7a75bb6
runtime="${RUNNER_TEMP:?}/dz07-ollama"
archive="$runtime/ollama-linux-amd64.tar.zst"
mkdir -p "$runtime"
curl --fail --location --retry 3 --max-time 600 \
  "https://github.com/ollama/ollama/releases/download/v${version}/ollama-linux-amd64.tar.zst" \
  --output "$archive"
printf '%s  %s\n' "$sha" "$archive" | sha256sum --check
# Only CPU inference is used. Exclude GPU libraries to fit a standard runner.
tar --zstd -xf "$archive" -C "$runtime" \
  --exclude='lib/ollama/cuda*' --exclude='lib/ollama/rocm*' \
  --exclude='lib/ollama/vulkan*' --exclude='lib/ollama/mlx*'
rm -- "$archive"
test -x "$runtime/bin/ollama"
printf 'OLLAMA_BINARY=%s\n' "$runtime/bin/ollama" >> "$GITHUB_ENV"
