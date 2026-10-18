#!/usr/bin/env python3
"""Package an actual Unity export for Xcode Cloud without committing engine binaries.
The payload must be uploaded as a GitHub release asset by the repo owner/CI.
The generated bootstrap checks SHA-256 before extracting anything.
"""
import argparse,hashlib,pathlib,shutil,tarfile,urllib.parse,secrets,subprocess,os
p=argparse.ArgumentParser();p.add_argument('--payload-url',required=True);p.add_argument('--password-file',type=pathlib.Path,default=pathlib.Path.home()/'.private_keys/motorcoast/ci-payload-password');args=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1];source=root/'Build/iOS';output=root/'Build/cloud-source'
assert (source/'Unity-iPhone.xcodeproj/project.pbxproj').is_file(),'Export Unity to Build/iOS first'
assert (source/'Data').is_dir(),'No Unity game data in export'
u=urllib.parse.urlsplit(args.payload_url)
assert u.scheme=='https' and u.netloc=='github.com' and u.path.startswith('/zanderrroff/tesla-roadster-drive/releases/download/'),'Use a release asset in this repository'
assert not u.query and not u.fragment
(root/'artifacts').mkdir(exist_ok=True);archive=root/'artifacts/xcode-payload.tar.gz'
with tarfile.open(archive,'w:gz',compresslevel=5) as t:
    for file in sorted(source.iterdir()):
        if 'DoNotShip' not in file.name and file.name!='ci_scripts':t.add(file,arcname=file.name)
checksum=hashlib.sha256()
with archive.open('rb') as payload:
    for block in iter(lambda:payload.read(1024*1024),b''):
        checksum.update(block)
plain_digest=checksum.hexdigest()
# A generated Xcode export includes Unity's build toolchain. Keep it encrypted
# at rest on public hosting; only the owner's CI receives the decryption secret.
args.password_file.parent.mkdir(parents=True,exist_ok=True)
if not args.password_file.exists():
    fd=os.open(args.password_file,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o600)
    with os.fdopen(fd,'w') as key:key.write(secrets.token_urlsafe(48))
encrypted=archive.with_suffix(archive.suffix+'.enc')
subprocess.run(['openssl','enc','-aes-256-cbc','-salt','-pbkdf2','-iter','200000','-in',str(archive),'-out',str(encrypted),'-pass','file:'+str(args.password_file)],check=True)
archive.unlink();archive=encrypted;checksum=hashlib.sha256()
with archive.open('rb') as payload:
    for block in iter(lambda:payload.read(1024*1024),b''):checksum.update(block)
digest=checksum.hexdigest()
output.mkdir(parents=True,exist_ok=True)
shutil.copytree(source/'Unity-iPhone.xcodeproj',output/'Unity-iPhone.xcodeproj',dirs_exist_ok=True)
shutil.copyfile(source/'Info.plist',output/'Info.plist')
(output/'ci_scripts').mkdir(exist_ok=True)
# URL came from a strict repository URL allowlist; shell quoting still escapes single quotes.
quote=lambda s:"'"+s.replace("'","'\\''")+"'"
script=f'''#!/bin/sh
set -eu
cd "$CI_PRIMARY_REPOSITORY_PATH"
PAYLOAD="${{TMPDIR:-/tmp}}/motorcoast-xcode-payload.tar.gz.enc"
: "${{MOTORCOAST_PAYLOAD_PASSWORD:?Configure the private CI payload password}}"
curl --fail --location --retry 3 {quote(args.payload_url)} --output "$PAYLOAD"
printf '%s  %s\\n' {quote(digest)} "$PAYLOAD" | shasum -a 256 --check --status
openssl enc -d -aes-256-cbc -pbkdf2 -iter 200000 -in "$PAYLOAD" -out "$PAYLOAD.decoded" -pass env:MOTORCOAST_PAYLOAD_PASSWORD
printf '%s  %s\\n' {quote(plain_digest)} "$PAYLOAD.decoded" | shasum -a 256 --check --status
tar -xzf "$PAYLOAD.decoded" -C "$CI_PRIMARY_REPOSITORY_PATH"
rm -f "$PAYLOAD" "$PAYLOAD.decoded"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $CI_BUILD_NUMBER" "$CI_PRIMARY_REPOSITORY_PATH/Info.plist"
echo "Unity player restored and verified for Xcode Cloud build $CI_BUILD_NUMBER"
'''
scriptpath=output/'ci_scripts/ci_post_clone.sh';scriptpath.write_text(script);scriptpath.chmod(0o755)
(output/'payload.sha256').write_text(digest+'\n')
# A fallback without Apple credentials can compile on a current SDK while the one-time
# Xcode Cloud product onboarding is pending. Signing stays on the owner's Mac.
workflows=output/'.github/workflows';workflows.mkdir(parents=True,exist_ok=True)
(workflows/'ios-archive.yml').write_text('''name: Native iPhone archive
on:
  push:
    branches: [ios-xcode-export]
  workflow_dispatch:
permissions:
  contents: read
concurrency:
  group: motorcoast-ios-archive
  cancel-in-progress: true
jobs:
  archive:
    runs-on: macos-26
    timeout-minutes: 75
    steps:
      - uses: actions/checkout@v4
      - name: Restore verified Unity export
        env:
          CI_PRIMARY_REPOSITORY_PATH: ${{ github.workspace }}
          CI_BUILD_NUMBER: ${{ github.run_number }}
          MOTORCOAST_PAYLOAD_PASSWORD: ${{ secrets.MOTORCOAST_PAYLOAD_PASSWORD }}
        run: sh ci_scripts/ci_post_clone.sh
      - name: Archive native ARM64 player
        run: |
          set -euo pipefail
          xcodebuild -version
          SDK_VERSION=$(xcrun --sdk iphoneos --show-sdk-version)
          test "${SDK_VERSION%%.*}" -ge 26
          xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone \\
            -configuration Release -destination 'generic/platform=iOS' \\
            -archivePath "$RUNNER_TEMP/MotorCoast.xcarchive" \\
            CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO CODE_SIGN_IDENTITY='' \\
            archive > xcode-archive.log 2>&1
          tar -czf unsigned-archive.tar.gz -C "$RUNNER_TEMP" MotorCoast.xcarchive
      - uses: actions/upload-artifact@v4
        with:
          name: MotorCoast-unsigned-archive
          path: unsigned-archive.tar.gz
          compression-level: 0
          retention-days: 7
      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: iOS-build-log
          path: xcode-archive.log
          retention-days: 7
''')
print('Payload:',archive);print('SHA-256:',digest);print('Cloud repository contents:',output)
print('Upload the payload to the exact URL, then commit cloud-source to ios-xcode-export. The CI payload password must be configured as MOTORCOAST_PAYLOAD_PASSWORD. No build was launched.')
