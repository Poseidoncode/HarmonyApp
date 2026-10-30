# Harmony Prompts — macOS & Windows

Offline AI prompt templates with two native apps: SwiftUI on macOS and C# / WPF on Windows. Shared JSON templates, five dynamic field types, live preview, and clipboard actions.

- **macOS:** existing menu bar app and installation below; see [`HarmonyPrompts/README.md`](HarmonyPrompts/README.md).
- **Windows:** new .NET 10 native app with notification-area access; see [`Windows/README.md`](Windows/README.md) for running, building, and portable ZIP packaging.
- **Verification:** [`Windows/IMPLEMENTATION_STATUS.md`](Windows/IMPLEMENTATION_STATUS.md) records actual results and untested environments. Windows Auto Paste is **off by default** pending safety acceptance; Settings permits experimental session-only opt-in. Copy + manual Ctrl+V is the recommended mode.

## Windows Quick Start

The locally generated portable archive is `Windows/artifacts/HarmonyPrompts-1.0.0-win-x64.zip` (build output, not committed or uploaded). Extract **all** files into a writable folder and run `HarmonyPrompts.exe`; keep `Resources/templates.json` alongside it. The self-contained archive does not require a separately installed .NET runtime. It is unsigned, so Windows may show a publisher warning; do not disable OS protections.

For development on Windows with `just`, Git Bash and the .NET 10 SDK, run `just dev` from the repository root. It restores/builds and launches the Debug app. It uses `dotnet` on PATH, or the user-local SDK at `%LOCALAPPDATA%\HarmonyBuild\dotnet` when PATH has no `dotnet`. Closing the window leaves the app in the tray; choose **Exit** there before restarting development.

To build from source on Windows with the .NET 10 SDK:

```powershell
dotnet restore Windows/HarmonyPrompts.slnx --locked-mode
dotnet build Windows/HarmonyPrompts.slnx -c Release --no-restore
dotnet test Windows/HarmonyPrompts.Tests/HarmonyPrompts.Tests.csproj -c Release --no-build --no-restore
dotnet run --project Windows/HarmonyPrompts.Windows -c Release --no-build
```

Windows templates live in `%APPDATA%\HarmonyPrompts\templates.json`. Imports replace the whole collection only after validation and a successful disk write. Closing the window hides it to the notification area; use the tray menu **Exit** to quit. No Windows release has been uploaded.

## macOS

The following screenshots and platform-specific shortcuts describe the unchanged macOS app.

## Screenshots

| Template Selection & Configuration | Real-Time Preview & Quick Paste |
| :---: | :---: |
| <img src="Assets/Picture1.png" alt="Template Selection & Parameters" width="400" /> | <img src="Assets/Picture2.png" alt="Rendered Output & Copy Actions" width="400" /> |
| Browse and search prompt templates, then configure dynamic parameter fields (language, code snippet, focus area, etc.). | Live preview of rendered prompt with character/word count, supporting one-click copy (`⌘C`) and auto-paste (`⌘⌥V`). |

## Features

- **Menu Bar Quick Access**: Resides in the macOS menu bar for fast access across Cursor, VS Code, browsers, Terminal, or any other application.
- **Dynamic Prompt Templates**: Supports multi-variable parameter configurations and custom form controls (text input, multiline text, dropdown selection).
- **Live Preview & Statistics**: Real-time prompt output rendering with live character and word count tracking.
- **Instant Injection (Copy & Paste)**: One-click clipboard copy (`⌘C`) or direct auto-paste into active frontmost application (`⌘⌥V`).
- **Flexible Template Management**: Support importing templates via JSON or directly editing the local `templates.json` configuration.

## Installation

### One-line Install (Recommended)

Install and build Harmony Prompts directly to `/Applications` via curl:

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

Or open `HarmonyPrompts/HarmonyPrompts.xcodeproj` in Xcode and press Run (`⌘R`).

<details>
<summary>Troubleshooting Xcode setup</summary>

If you encounter `xcodebuild` path or license errors, run:

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -license accept
xcodebuild -runFirstLaunch
```

</details>

## Quick Start

1. Launch **Harmony Prompts** from `/Applications` or Spotlight.
2. Click the menu bar bubble icon.
3. Pick a template, fill in parameters, and click **Copy** (`⌘C`) or **Copy & Paste** (`⌘⌥V`).

Run from **`/Applications/Harmony Prompts.app`** (not the `build/` folder) so Accessibility permission stays valid.

## Accessibility (Copy & Paste)

**Copy & Paste** requires **System Settings -> Privacy & Security -> Accessibility**.

Enable **Harmony Prompts** once. If you see duplicate entries, remove old ones pointing at `build/` paths.

Until permission is granted, use **Copy** + manual **⌘V**.

## Project Layout

```
HarmonyPrompts/             # Original macOS project (unchanged)
Windows/                   # WPF app, Core, tests, local desktop harness, docs
shared/                    # Compatible templates, JSON schema, golden fixtures
```

## Security & Privacy

Harmony Prompts operates 100% locally with zero data collection, no telemetry, and no outbound network calls. All templates and prompt evaluations remain strictly on your local machine.

For security policies, vulnerability reporting, and threat model details, see [SECURITY.md](SECURITY.md).
