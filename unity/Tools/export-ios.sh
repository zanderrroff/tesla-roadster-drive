#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="$(awk '/m_EditorVersion:/{print $2}' "$ROOT/MotorCoast/ProjectSettings/ProjectVersion.txt")"
EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
if [ ! -x "$EDITOR" ]; then
  echo "Unity $VERSION is not installed. Install it with iOS Build Support and activate it in Unity Hub." >&2
  exit 2
fi
if [ ! -d "$(dirname "$EDITOR")/../PlaybackEngines/iOSSupport" ]; then
  echo 'iOS Build Support module is missing.' >&2; exit 3
fi
mkdir -p "$ROOT/artifacts"
bash "$ROOT/Tools/build-editor-plugin.sh"
"$EDITOR" -batchmode -quit -projectPath "$ROOT/MotorCoast" -buildTarget iOS -executeMethod MotorCoast.Editor.BuildIOS.Prepare -logFile "$ROOT/artifacts/unity-prepare.log"
# Do not export a release if the Unity tests fail.
"$EDITOR" -batchmode -projectPath "$ROOT/MotorCoast" -buildTarget iOS -runTests -testPlatform EditMode -testResults "$ROOT/artifacts/unity-tests.xml" -logFile "$ROOT/artifacts/unity-tests.log"
python3 - "$ROOT/artifacts/unity-tests.xml" <<'PY'
import sys,xml.etree.ElementTree as E
r=E.parse(sys.argv[1]).getroot()
assert int(r.attrib.get('total','0'))>0, 'No Unity tests executed'
assert r.attrib.get('result')=='Passed', r.attrib
PY
"$EDITOR" -batchmode -quit -projectPath "$ROOT/MotorCoast" -buildTarget iOS -executeMethod MotorCoast.Editor.BuildIOS.Export -logFile "$ROOT/artifacts/unity-export.log"
echo "Xcode project: $ROOT/Build/iOS/Unity-iPhone.xcodeproj"
