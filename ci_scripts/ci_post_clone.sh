#!/bin/sh
set -eu
cd "$CI_PRIMARY_REPOSITORY_PATH"
PAYLOAD="${TMPDIR:-/tmp}/motorcoast-xcode-payload.tar.gz.enc"
: "${MOTORCOAST_PAYLOAD_PASSWORD:?Configure the private CI payload password}"
curl --fail --location --retry 3 'https://github.com/zanderrroff/tesla-roadster-drive/releases/download/ios-native-build-1/xcode-payload.tar.gz.enc' --output "$PAYLOAD"
printf '%s  %s\n' '79c17123aa7a20bcdf8d6f782d420a61f561fd848e4108a3d3816df04911c992' "$PAYLOAD" | shasum -a 256 --check --status
openssl enc -d -aes-256-cbc -pbkdf2 -iter 200000 -in "$PAYLOAD" -out "$PAYLOAD.decoded" -pass env:MOTORCOAST_PAYLOAD_PASSWORD
printf '%s  %s\n' 'e81680822f4753c5cd0796e185531d719edf17ed14d6bef2df46e2b81ba93c57' "$PAYLOAD.decoded" | shasum -a 256 --check --status
tar -xzf "$PAYLOAD.decoded" -C "$CI_PRIMARY_REPOSITORY_PATH"
rm -f "$PAYLOAD" "$PAYLOAD.decoded"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $CI_BUILD_NUMBER" "$CI_PRIMARY_REPOSITORY_PATH/Info.plist"
echo "Unity player restored and verified for Xcode Cloud build $CI_BUILD_NUMBER"
