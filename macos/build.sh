#!/bin/bash
set -euo pipefail

if [[ "$(uname -s)" != Darwin ]]; then
    echo "请在安装了 Xcode 命令行工具的 macOS 上运行此脚本。" >&2
    exit 1
fi

root="$(cd "$(dirname "$0")" && pwd)"
output="$root/build"
mkdir -p "$output"
stage="$(mktemp -d "$output/.meowkeys-build.XXXXXX")"
trap 'rm -rf "$stage"' EXIT
export COPYFILE_DISABLE=1

plist="$root/Info.plist"
version="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$plist")"
minimum="$(/usr/libexec/PlistBuddy -c 'Print :LSMinimumSystemVersion' "$plist")"
app_name="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleName' "$plist").app"
executable="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$plist")"
zip_name="键盘喵-${version}-macOS.zip"
app="$stage/payload/$app_name"
contents="$app/Contents"
binary="$contents/MacOS/$executable"

sdk="$(xcrun --sdk macosx --show-sdk-path)"
mkdir -p "$contents/MacOS"
for arch in arm64 x86_64; do
    xcrun --sdk macosx clang -arch "$arch" -isysroot "$sdk" \
        "-mmacosx-version-min=$minimum" -fobjc-arc -fblocks -O2 -g0 \
        -Werror=unguarded-availability-new -Werror=implicit-function-declaration \
        "$root/app/"*.m -framework Cocoa -framework ApplicationServices \
        -framework CoreGraphics -o "$stage/$arch"
done
xcrun lipo -create "$stage/arm64" "$stage/x86_64" -output "$binary"
xcrun lipo "$binary" -verify_arch arm64 x86_64

cp "$plist" "$contents/Info.plist"
printf 'APPL????' > "$contents/PkgInfo"
ditto --norsrc --noextattr "$root/resources" "$contents/Resources"
codesign --force --sign - "$app"
codesign --verify --strict --all-architectures "$app"

# keepParent puts 键盘喵.app at the top level of the zip; sequesterRsrc keeps
# Finder-built archives clean for users on other machines.
ditto -c -k --sequesterRsrc --keepParent "$app" "$stage/$zip_name"

if [[ -e "$output/$app_name" ]]; then
    mv "$output/$app_name" "$stage/previous.app"
fi
mv "$app" "$output/$app_name"
mv -f "$stage/$zip_name" "$output/$zip_name"
printf '%s\n' "$output/$app_name" "$output/$zip_name"
