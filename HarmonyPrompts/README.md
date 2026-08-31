# Harmony Prompts — macOS menu bar app

SwiftUI desktop app for quick AI prompt templates. Works in Cursor, browser, Terminal, anywhere.

## Screenshots

| Template Selection & Configuration | Real-Time Preview & Quick Paste |
| :---: | :---: |
| <img src="../Assets/Picture1.png" alt="Template Selection & Parameters" width="400" /> | <img src="../Assets/Picture2.png" alt="Rendered Output & Copy Actions" width="400" /> |
| Browse and search prompt templates, then configure dynamic parameter fields. | Live preview of rendered prompt with one-click copy (`⌘C`) and auto-paste (`⌘⌥V`). |

## Installation & Build

### One-line Install (Recommended)

```bash
curl -fsSL https://raw.githubusercontent.com/Poseidoncode/HarmonyApp/main/install.sh | bash
```

> **Requirement**: macOS 14.0+ with Xcode installed.

### Build from Source

```bash
git clone https://github.com/Poseidoncode/HarmonyApp.git
cd HarmonyApp
./install.sh
```

Or in Xcode: open `HarmonyPrompts.xcodeproj` -> Run (⌘R).

<details>
<summary>Troubleshooting Xcode setup</summary>

If you encounter `xcodebuild` path or license errors, run:

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -license accept
xcodebuild -runFirstLaunch
```

</details>

## Usage

1. Click the **text bubble** icon in the menu bar
2. Pick a template, fill fields
3. **Copy** — paste manually with ⌘V anywhere
4. **Copy & Paste** — auto-pastes into the frontmost app (needs Accessibility permission)

Settings (Harmony Prompts -> Settings…): templates folder, Accessibility help.

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

## Security & Privacy

Harmony Prompts operates 100% offline and locally. See [SECURITY.md](../SECURITY.md) for vulnerability reporting and architecture details.

