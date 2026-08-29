# Harmony Prompts — macOS

Quick-insert AI prompt templates via a menu bar app. Works in Cursor, browser, Terminal, anywhere.

See [`HarmonyPrompts/README.md`](HarmonyPrompts/README.md) for details.

## Quick start

```bash
just install
```

Click the menu bar bubble → pick template → **Copy** or **Copy & Paste**.

Run from **`/Applications/Harmony Prompts.app`** (not the `build/` folder) so Accessibility permission stays valid.

## Accessibility (Copy & Paste)

**Copy & Paste** needs **System Settings → Privacy & Security → Accessibility**.

Enable **Harmony Prompts** once. If you see duplicate entries, remove old ones pointing at `build/` paths.

Until permission is granted, use **Copy** + manual **⌘V**.

## Project layout

```
HarmonyPrompts/
├── HarmonyPrompts/          # SwiftUI app source
├── HarmonyPrompts.xcodeproj
└── scripts/                 # Icon build scripts
```
