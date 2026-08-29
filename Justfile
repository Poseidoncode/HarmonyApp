# Harmony Prompts — macOS app
set shell := ["bash", "-cu"]

default:
    @just --list

app_dir := "HarmonyPrompts"
app_scheme := "HarmonyPrompts"

[private]
build:
    python3 {{app_dir}}/scripts/generate-icon.py
    bash {{app_dir}}/scripts/build-app-icon.sh
    xcodebuild -project {{app_dir}}/HarmonyPrompts.xcodeproj -scheme {{app_scheme}} -configuration Release -derivedDataPath {{app_dir}}/build build

# Build, install to /Applications, and launch
install: build
    pkill -f "Harmony Prompts" 2>/dev/null || true
    rm -rf "/Applications/Harmony Prompts.app"
    cp -R {{app_dir}}/build/Build/Products/Release/Harmony\ Prompts.app "/Applications/"
    /System/Library/Frameworks/CoreServices.framework/Versions/Current/Frameworks/LaunchServices.framework/Versions/Current/Support/lsregister -f -R -trusted "/Applications/Harmony Prompts.app"
    open "/Applications/Harmony Prompts.app"
