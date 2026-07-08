#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MACOS_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$MACOS_ROOT/.." && pwd)"
PROJECT="$MACOS_ROOT/src/YBBvideozip.Mac.csproj"
DIST="$MACOS_ROOT/dist"
PUBLISH="$DIST/osx-arm64/publish"
APP="$DIST/YBBvideozip.app"
CONTENTS="$APP/Contents"
MACOS_DIR="$CONTENTS/MacOS"
RESOURCES="$CONTENTS/Resources"
ICON_PNG="$MACOS_ROOT/assets/YBBvideozip-icon.png"
ICONSET="$DIST/YBBvideozip.iconset"
ICNS="$RESOURCES/YBBvideozip.icns"
REPO_DOTNET="$REPO_ROOT/tools/dotnet/dotnet"

if [[ -x "$REPO_DOTNET" ]]; then
  DOTNET="$REPO_DOTNET"
elif command -v dotnet >/dev/null 2>&1; then
  DOTNET="$(command -v dotnet)"
else
  echo "Missing dotnet. Install .NET 8 SDK first."
  exit 1
fi

if ! "$DOTNET" --list-sdks | grep -q '^8\.'; then
  echo "Missing .NET 8 SDK. Install .NET 8 SDK first."
  exit 1
fi

rm -rf "$PUBLISH" "$APP" "$ICONSET"
mkdir -p "$PUBLISH" "$MACOS_DIR" "$RESOURCES"

"$DOTNET" publish "$PROJECT" \
  --configuration Release \
  --runtime osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:PublishTrimmed=false \
  --output "$PUBLISH"

cp -R "$PUBLISH/"* "$MACOS_DIR/"
chmod +x "$MACOS_DIR/YBBvideozip"

if [[ -f "$ICON_PNG" ]] && command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1; then
  mkdir -p "$ICONSET"
  sips -z 16 16 "$ICON_PNG" --out "$ICONSET/icon_16x16.png" >/dev/null
  sips -z 32 32 "$ICON_PNG" --out "$ICONSET/icon_16x16@2x.png" >/dev/null
  sips -z 32 32 "$ICON_PNG" --out "$ICONSET/icon_32x32.png" >/dev/null
  sips -z 64 64 "$ICON_PNG" --out "$ICONSET/icon_32x32@2x.png" >/dev/null
  sips -z 128 128 "$ICON_PNG" --out "$ICONSET/icon_128x128.png" >/dev/null
  sips -z 256 256 "$ICON_PNG" --out "$ICONSET/icon_128x128@2x.png" >/dev/null
  sips -z 256 256 "$ICON_PNG" --out "$ICONSET/icon_256x256.png" >/dev/null
  sips -z 512 512 "$ICON_PNG" --out "$ICONSET/icon_256x256@2x.png" >/dev/null
  sips -z 512 512 "$ICON_PNG" --out "$ICONSET/icon_512x512.png" >/dev/null
  sips -z 1024 1024 "$ICON_PNG" --out "$ICONSET/icon_512x512@2x.png" >/dev/null
  iconutil -c icns "$ICONSET" -o "$ICNS"
fi

if [[ ! -f "$ICNS" ]]; then
  echo "Missing app icon. Expected: $ICNS" >&2
  exit 1
fi

cat > "$CONTENTS/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>YBBvideozip</string>
  <key>CFBundleIdentifier</key>
  <string>cn.shenlouar.ybbvideozip</string>
  <key>CFBundleName</key>
  <string>YBBvideozip</string>
  <key>CFBundleDisplayName</key>
  <string>YBB视频压缩</string>
  <key>CFBundleVersion</key>
  <string>1.1.1</string>
  <key>CFBundleShortVersionString</key>
  <string>1.1.1</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>CFBundleIconFile</key>
  <string>YBBvideozip</string>
</dict>
</plist>
PLIST

echo "FFmpeg is not bundled. The app will download it from runtime-manifest.json on first run."

if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$APP"
  echo "Ad-hoc signed local app."
fi

echo "Created: $APP"
echo "Run: open \"$APP\""
