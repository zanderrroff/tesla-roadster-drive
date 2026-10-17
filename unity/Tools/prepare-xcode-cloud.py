#!/usr/bin/env python3
"""Package an actual Unity export for Xcode Cloud without committing engine binaries.
The payload must be uploaded as a GitHub release asset by the repo owner/CI.
The generated bootstrap checks SHA-256 before extracting anything.
"""
import argparse,hashlib,pathlib,shutil,tarfile,urllib.parse
p=argparse.ArgumentParser();p.add_argument('--payload-url',required=True);args=p.parse_args()
root=pathlib.Path(__file__).resolve().parents[1];source=root/'Build/iOS';output=root/'Build/cloud-source'
assert (source/'Unity-iPhone.xcodeproj/project.pbxproj').is_file(),'Export Unity to Build/iOS first'
assert (source/'Data').is_dir(),'No Unity game data in export'
u=urllib.parse.urlsplit(args.payload_url)
assert u.scheme=='https' and u.netloc=='github.com' and u.path.startswith('/zanderrroff/tesla-roadster-drive/releases/download/'),'Use a release asset in this repository'
assert not u.query and not u.fragment
(root/'artifacts').mkdir(exist_ok=True);archive=root/'artifacts/xcode-payload.tar.gz'
with tarfile.open(archive,'w:gz',compresslevel=5) as t:
    for file in sorted(source.iterdir()):t.add(file,arcname=file.name)
digest=hashlib.sha256(archive.read_bytes()).hexdigest()
output.mkdir(parents=True,exist_ok=True)
shutil.copytree(source/'Unity-iPhone.xcodeproj',output/'Unity-iPhone.xcodeproj',dirs_exist_ok=True)
shutil.copyfile(source/'Info.plist',output/'Info.plist')
(output/'ci_scripts').mkdir(exist_ok=True)
# URL came from a strict repository URL allowlist; shell quoting still escapes single quotes.
quote=lambda s:"'"+s.replace("'","'\\''")+"'"
script=f'''#!/bin/sh
set -eu
cd "$CI_PRIMARY_REPOSITORY_PATH"
PAYLOAD="${{TMPDIR:-/tmp}}/motorcoast-xcode-payload.tar.gz"
curl --fail --location --retry 3 {quote(args.payload_url)} --output "$PAYLOAD"
printf '%s  %s\\n' {quote(digest)} "$PAYLOAD" | shasum -a 256 --check --status
tar -xzf "$PAYLOAD" -C "$CI_PRIMARY_REPOSITORY_PATH"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $CI_BUILD_NUMBER" "$CI_PRIMARY_REPOSITORY_PATH/Info.plist"
echo "Unity player restored and verified for Xcode Cloud build $CI_BUILD_NUMBER"
'''
scriptpath=output/'ci_scripts/ci_post_clone.sh';scriptpath.write_text(script);scriptpath.chmod(0o755)
(output/'payload.sha256').write_text(digest+'\n')
print('Payload:',archive);print('SHA-256:',digest);print('Cloud repository contents:',output)
print('Upload the payload to the exact URL, then commit cloud-source to ios-xcode-export. No build was launched.')
