#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/artifacts/native-tests"
mkdir -p "$OUT"
xcrun clang++ -std=c++17 -Wall -Wextra -Werror -fsanitize=address,undefined -g "$ROOT/Tests/drive-core-test.cpp" "$ROOT/MotorCoast/Assets/Plugins/iOS/DriveCore.cpp" -o "$OUT/drive-core-test"
"$OUT/drive-core-test"
