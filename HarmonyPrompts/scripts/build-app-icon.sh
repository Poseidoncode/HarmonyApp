#!/usr/bin/env bash
# Build a complete AppIcon.icns (actool sometimes omits large sizes).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
ICONSET_SRC="${ROOT}/HarmonyPrompts/Assets.xcassets/AppIcon.appiconset"
SOURCE="${ICONSET_SRC}/icon_512x512@2x.png"

if [[ ! -f "${SOURCE}" ]]; then
  SOURCE="${ICONSET_SRC}/icon_1024x1024.png"
fi

if [[ ! -f "${SOURCE}" ]]; then
  echo "error: no 1024px source icon in AppIcon.appiconset" >&2
  exit 1
fi

WORK="$(mktemp -d)"
trap 'rm -rf "${WORK}"' EXIT

ICONSET="${WORK}/AppIcon.iconset"
mkdir -p "${ICONSET}"

declare -a specs=(
  "16:icon_16x16.png"
  "32:icon_16x16@2x.png"
  "32:icon_32x32.png"
  "64:icon_32x32@2x.png"
  "128:icon_128x128.png"
  "256:icon_128x128@2x.png"
  "256:icon_256x256.png"
  "512:icon_256x256@2x.png"
  "512:icon_512x512.png"
  "1024:icon_512x512@2x.png"
)

for spec in "${specs[@]}"; do
  size="${spec%%:*}"
  name="${spec#*:}"
  sips -z "${size}" "${size}" "${SOURCE}" --out "${ICONSET}/${name}" >/dev/null
  cp "${ICONSET}/${name}" "${ICONSET_SRC}/${name}"
done

# Remove unassigned extras that confuse actool.
rm -f "${ICONSET_SRC}/icon_1024x1024.png" "${ICONSET_SRC}/icon_64x64.png"

iconutil -c icns "${ICONSET}" -o "${ICONSET_SRC}/AppIcon.icns"

if [[ -n "${BUILT_PRODUCTS_DIR:-}" && -n "${UNLOCALIZED_RESOURCES_FOLDER_PATH:-}" ]]; then
  DEST="${BUILT_PRODUCTS_DIR}/${UNLOCALIZED_RESOURCES_FOLDER_PATH}/AppIcon.icns"
  mkdir -p "$(dirname "${DEST}")"
  cp "${ICONSET_SRC}/AppIcon.icns" "${DEST}"
  echo "Installed AppIcon.icns -> ${DEST}"
else
  echo "Built ${ICONSET_SRC}/AppIcon.icns"
fi
