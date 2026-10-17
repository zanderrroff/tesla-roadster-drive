#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/MotorCoast/Assets/Plugins/macOS"
mkdir -p "$OUT"
xcrun clang++ -std=c++17 -O2 -dynamiclib -arch x86_64 -arch arm64 -mmacosx-version-min=11.0 "$ROOT/MotorCoast/Assets/Plugins/iOS/DriveCore.cpp" -o "$OUT/libMotorCoastNative.dylib"
echo 'Built universal macOS driving plugin.'
