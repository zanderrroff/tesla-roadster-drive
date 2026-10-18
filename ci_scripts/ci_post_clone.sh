#!/bin/sh
set -eu
cd "$CI_PRIMARY_REPOSITORY_PATH"
PAYLOAD="${TMPDIR:-/tmp}/motorcoast-xcode-payload.tar.gz.enc"
: "${MOTORCOAST_PAYLOAD_PASSWORD:?Configure the private CI payload password}"
curl --fail --location --retry 3 'https://github.com/zanderrroff/tesla-roadster-drive/releases/download/ios-native-build-2/xcode-payload.tar.gz.enc' --output "$PAYLOAD"
printf '%s  %s\n' 'aa404706219e7f9a2fdb12a013c7ba6bb1357097f5bd44f12d974f9286ff801c' "$PAYLOAD" | shasum -a 256 --check --status
openssl enc -d -aes-256-cbc -pbkdf2 -iter 200000 -in "$PAYLOAD" -out "$PAYLOAD.decoded" -pass env:MOTORCOAST_PAYLOAD_PASSWORD
printf '%s  %s\n' 'b3a9937b67af4a181a1a986a855e18ac96c6afd27eeb217e29ded32f925c0594' "$PAYLOAD.decoded" | shasum -a 256 --check --status
tar -xzf "$PAYLOAD.decoded" -C "$CI_PRIMARY_REPOSITORY_PATH"
rm -f "$PAYLOAD" "$PAYLOAD.decoded"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $CI_BUILD_NUMBER" "$CI_PRIMARY_REPOSITORY_PATH/Info.plist"
echo "Unity player restored and verified for Xcode Cloud build $CI_BUILD_NUMBER"
