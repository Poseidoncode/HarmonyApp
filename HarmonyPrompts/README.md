# Harmony Prompts — macOS menu bar app

SwiftUI desktop app for quick AI prompt templates. Works in Cursor, browser, Terminal, anywhere.

## Build & run

```bash
just install
```

Or in Xcode: open `HarmonyPrompts.xcodeproj` → Run (⌘R).

## Usage

1. Click the **text bubble** icon in the menu bar
2. Pick a template, fill fields
3. **Copy** — paste manually with ⌘V anywhere
4. **Copy & Paste** — auto-pastes into the frontmost app (needs Accessibility permission)

Settings (Harmony Prompts → Settings…): templates folder, Accessibility help.

## Templates

- Default templates ship in the app bundle
- User copy: `~/Library/Application Support/HarmonyPrompts/templates.json`
- **Import JSON…** — load Harmony `templates.json` format
- **Open templates folder** — edit JSON directly

## Sync from Harmony

Use **Import JSON…** in the app with `../Harmony/resources/templates.json`, or copy that file to:

```
~/Library/Application Support/HarmonyPrompts/templates.json
```
