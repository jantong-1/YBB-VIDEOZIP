#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MACOS_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

dotnet test "$MACOS_ROOT/tests/YBBvideozip.Mac.Tests.csproj"

"$SCRIPT_DIR/package-local-app.sh"

open "$MACOS_ROOT/dist/YBBvideozip.app"
