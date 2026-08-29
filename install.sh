#!/usr/bin/env bash
set -e

echo "=== Installing Harmony Prompts ==="

# Check for Xcode / xcodebuild
if ! command -v xcodebuild >/dev/null 2>&1; then
    echo "Error: xcodebuild is required to build Harmony Prompts. Please install Xcode and Command Line Tools."
    exit 1
fi

TEMP_DIR=""
cleanup() {
    if [ -n "$TEMP_DIR" ] && [ -d "$TEMP_DIR" ]; then
        rm -rf "$TEMP_DIR"
    fi
}
trap cleanup EXIT

# Check if running in the source directory
if [ -f "HarmonyPrompts/HarmonyPrompts.xcodeproj/project.pbxproj" ]; then
    PROJECT_DIR="$(pwd)/HarmonyPrompts"
elif [ -f "HarmonyPrompts.xcodeproj/project.pbxproj" ]; then
    PROJECT_DIR="$(pwd)"
else
    echo "Downloading source..."
    TEMP_DIR=$(mktemp -d)
    git clone --depth 1 https://github.com/Poseidoncode/HarmonyApp.git "$TEMP_DIR/HarmonyApp"
    PROJECT_DIR="$TEMP_DIR/HarmonyApp/HarmonyPrompts"
fi

echo "Building Harmony Prompts (Release)..."
BUILD_DIR="$PROJECT_DIR/build"

xcodebuild -project "$PROJECT_DIR/HarmonyPrompts.xcodeproj" \
    -scheme HarmonyPrompts \
    -configuration Release \
    -derivedDataPath "$BUILD_DIR" \
    CONFIGURATION_BUILD_DIR="$BUILD_DIR/Release" \
    > /dev/null

echo "Installing to /Applications/Harmony Prompts.app..."
rm -rf "/Applications/Harmony Prompts.app"
cp -R "$BUILD_DIR/Release/Harmony Prompts.app" "/Applications/"

echo "=== Installation complete ==="
echo "Harmony Prompts has been installed to /Applications/Harmony Prompts.app"
echo "Launch it from Applications or Spotlight, and grant Accessibility permissions in System Settings when prompted."
