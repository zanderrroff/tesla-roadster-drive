#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="${MOTORCOAST_XCODE_OUTPUT:-$ROOT/Build/iOS}"
[ -d "$PROJECT/Unity-iPhone.xcodeproj" ] || { echo 'Run export-ios.sh with activated Unity first.' >&2; exit 2; }
mkdir -p "$ROOT/artifacts"
AUTH=()
if [ -n "${ASC_KEY_PATH:-}" ]; then
  : "${ASC_KEY_ID:?ASC_KEY_ID is required}" "${ASC_ISSUER_ID:?ASC_ISSUER_ID is required}"
  AUTH=(-authenticationKeyPath "$ASC_KEY_PATH" -authenticationKeyID "$ASC_KEY_ID" -authenticationKeyIssuerID "$ASC_ISSUER_ID")
fi
xcodebuild -project "$PROJECT/Unity-iPhone.xcodeproj" -scheme Unity-iPhone -configuration Release -destination 'generic/platform=iOS' -archivePath "$ROOT/artifacts/MotorCoast.xcarchive" -allowProvisioningUpdates "${AUTH[@]}" DEVELOPMENT_TEAM="${APPLE_TEAM_ID:-U8QHYG2G45}" archive > "$ROOT/artifacts/xcode-archive.log" 2>&1
xcodebuild -exportArchive -archivePath "$ROOT/artifacts/MotorCoast.xcarchive" -exportOptionsPlist "$ROOT/Tools/ExportOptions.plist" -exportPath "$ROOT/artifacts/ipa" -allowProvisioningUpdates "${AUTH[@]}" > "$ROOT/artifacts/xcode-export.log" 2>&1
python3 "$ROOT/Tools/verify-ipa.py" "$ROOT/artifacts/ipa/Motor Coast.ipa"
