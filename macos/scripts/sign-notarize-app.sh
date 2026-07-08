#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MACOS_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DIST="$MACOS_ROOT/dist"
APP="$DIST/YBBvideozip.app"
MACOS_DIR="$APP/Contents/MacOS"
MAIN_EXECUTABLE="$MACOS_DIR/YBBvideozip"
ENTITLEMENTS="$MACOS_ROOT/entitlements.plist"
FINAL_ZIP="$DIST/YBBvideozip-macos-arm64.zip"
NOTARY_ZIP="$DIST/YBBvideozip-macos-arm64-notary.zip"
DEFAULT_PROFILE="YBB_NOTARY"

if [[ ! -d "$APP" ]]; then
  "$SCRIPT_DIR/package-local-app.sh"
fi

if [[ ! -d "$APP" ]]; then
  echo "Missing app bundle: $APP" >&2
  exit 1
fi

if [[ ! -f "$ENTITLEMENTS" ]]; then
  echo "Missing entitlements file: $ENTITLEMENTS" >&2
  exit 1
fi

if [[ -n "${YBB_CODESIGN_IDENTITY:-}" ]]; then
  IDENTITY="$YBB_CODESIGN_IDENTITY"
else
  IDENTITY="$(security find-identity -v -p codesigning | awk -F '"' '/Developer ID Application:/ { print $2; exit }')"
fi

if [[ -z "$IDENTITY" ]]; then
  echo "Missing Developer ID Application signing identity." >&2
  echo "Create one in Xcode: Settings > Accounts > Team > Manage Certificates." >&2
  exit 1
fi

TEAM_ID="${YBB_NOTARY_TEAM_ID:-${YBB_TEAM_ID:-}}"
if [[ -z "$TEAM_ID" ]]; then
  TEAM_ID="$(printf '%s\n' "$IDENTITY" | sed -n 's/.*(\([A-Z0-9]\{10\}\))$/\1/p')"
fi

echo "Signing with: $IDENTITY"

while IFS= read -r -d '' code_file; do
  if [[ "$code_file" == "$MAIN_EXECUTABLE" ]]; then
    continue
  fi

  if ! file "$code_file" | grep -q "Mach-O"; then
    continue
  fi

  codesign --force --timestamp --options runtime --sign "$IDENTITY" "$code_file"
done < <(find "$MACOS_DIR" -type f -print0)

codesign --force --timestamp --options runtime --entitlements "$ENTITLEMENTS" --sign "$IDENTITY" "$MAIN_EXECUTABLE"
codesign --force --timestamp --options runtime --entitlements "$ENTITLEMENTS" --sign "$IDENTITY" "$APP"
codesign --verify --deep --strict --verbose=4 "$APP"

if [[ "${YBB_SKIP_NOTARIZE:-}" == "1" ]]; then
  rm -f "$FINAL_ZIP"
  ditto -c -k --sequesterRsrc --keepParent "$APP" "$FINAL_ZIP"
  shasum -a 256 "$FINAL_ZIP"
  echo "Signed without notarization because YBB_SKIP_NOTARIZE=1."
  exit 0
fi

rm -f "$NOTARY_ZIP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$NOTARY_ZIP"

if [[ -n "${YBB_NOTARY_PROFILE:-}" ]]; then
  NOTARY_ARGS=(--keychain-profile "$YBB_NOTARY_PROFILE")
elif [[ -n "${YBB_NOTARY_APPLE_ID:-}" && -n "${YBB_NOTARY_PASSWORD:-}" ]]; then
  if [[ -z "$TEAM_ID" ]]; then
    echo "Missing team id. Set YBB_NOTARY_TEAM_ID." >&2
    exit 1
  fi
  NOTARY_ARGS=(--apple-id "$YBB_NOTARY_APPLE_ID" --password "$YBB_NOTARY_PASSWORD" --team-id "$TEAM_ID")
else
  NOTARY_ARGS=(--keychain-profile "$DEFAULT_PROFILE")
fi

if ! xcrun notarytool submit "$NOTARY_ZIP" "${NOTARY_ARGS[@]}" --wait; then
  echo "Notarization failed or credentials are missing." >&2
  if [[ "${NOTARY_ARGS[*]}" == *"--keychain-profile $DEFAULT_PROFILE"* ]]; then
    echo "Create the keychain profile on this Mac:" >&2
    echo "xcrun notarytool store-credentials \"$DEFAULT_PROFILE\" --apple-id \"YOUR_APPLE_ID_EMAIL\" --team-id \"$TEAM_ID\"" >&2
  fi
  exit 1
fi

xcrun stapler staple -v "$APP"
xcrun stapler validate -v "$APP"

if command -v syspolicy_check >/dev/null 2>&1; then
  syspolicy_check distribution "$APP"
else
  spctl --assess --type open --context context:primary-signature --verbose=4 "$APP"
fi

rm -f "$FINAL_ZIP" "$NOTARY_ZIP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$FINAL_ZIP"
shasum -a 256 "$FINAL_ZIP"
echo "Created notarized release: $FINAL_ZIP"
