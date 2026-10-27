#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="$(awk '/m_EditorVersion:/{print $2}' "$ROOT/MotorCoast/ProjectSettings/ProjectVersion.txt")"
EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
test -x "$EDITOR" || { echo "Unity $VERSION is missing." >&2; exit 2; }
mkdir -p "$ROOT/artifacts"
bash "$ROOT/Tools/build-editor-plugin.sh"
"$EDITOR" -batchmode -projectPath "$ROOT/MotorCoast" -buildTarget StandaloneOSX -runTests -testPlatform EditMode -testResults "$ROOT/artifacts/mac-tests.xml" -logFile "$ROOT/artifacts/mac-tests.log"
python3 - "$ROOT/artifacts/mac-tests.xml" <<'PY'
import sys,xml.etree.ElementTree as E
r=E.parse(sys.argv[1]).getroot()
assert int(r.attrib.get('total','0'))>0 and r.attrib.get('result')=='Passed', r.attrib
print('Unity macOS tests:',r.attrib['passed'],'passed')
PY
"$EDITOR" -batchmode -quit -projectPath "$ROOT/MotorCoast" -buildTarget StandaloneOSX -executeMethod MotorCoast.Editor.BuildMac.Export -logFile "$ROOT/artifacts/mac-export.log"
