# Harmony Prompts — Windows native app

C# / WPF / MVVM on **.NET 10**. The macOS SwiftUI app is unchanged. The app works offline: no accounts, telemetry, SDK analytics, auto-updater, startup registration, or application network requests.

> **Status:** source implementation, Release build, unit/fault tests, controlled Windows desktop smoke tests and self-contained publishing are available. See [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) for actual results. A complete external-app matrix, clean Windows 10/11 VMs, DPI/multi-monitor and macOS runtime verification remain open. No release has been uploaded. **Auto Paste is OFF by default and is not signed off for safety acceptance.** Settings offers an explicit experimental opt-in for the current session only. Prefer Copy + manual Ctrl+V.

## Run the portable build

1. Use a serviced Windows x64 version supported by [.NET 10](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), normally Windows 11. Windows 10 availability also depends on edition and servicing status; this is not a promise of support for every Windows 10 release.
2. Extract **all** of `Windows/artifacts/HarmonyPrompts-1.0.0-win-x64.zip` into a folder you can write to.
3. Run `HarmonyPrompts.exe`. Keep `Resources/templates.json` in the extracted `Resources` folder. This is a self-contained build: no separate .NET runtime is needed.
4. The archive is unsigned; Windows may display an unknown-publisher warning. Inspect and trust your local build; **do not disable SmartScreen, UAC or other protections**.

The ZIP is a local, ignored build artifact, not a checked-in binary or a hosted download. Portable ZIP was chosen rather than an installer to avoid registry/startup changes, elevation and untested installer upgrade behavior. To upgrade, exit from the tray and replace the extracted application folder. To uninstall, exit and delete that folder. Neither action deletes user templates. Delete `%APPDATA%\HarmonyPrompts` separately **only if you deliberately want to erase your templates**. There is no automatic updater.

## Use

- Focus the intended **input field** in another app before opening Harmony from its notification-area icon.
- Search template names/descriptions, select a template, and fill in the generated form. Supported fields are `text`, `textarea`, `select`, `radio` and `checkbox`.
- Preview updates immediately. Counts use grapheme clusters rather than UTF-16 length and Swift-compatible whitespace fixtures.
- **Copy** writes the generated preview to the clipboard. **Copy & Paste is disabled by default**. If you explicitly enable experimental Auto Paste for this session in Settings, it also tries to return to the captured exact window and send Ctrl+V. Restarting turns it off again.
- The displayed target is an application/PID, not proof that a particular editor control has focus. Inspect the destination after input is sent.
- **Import JSON…** replaces the entire collection. It does not merge. Invalid/empty imports or failed writes leave the existing collection intact.
- **Folder** opens `%APPDATA%\HarmonyPrompts`. You can edit `templates.json` there and use **Reload**. Unchanged templates retain in-progress form values. Switching to a different template resets that form to its defaults.
- Closing the main window hides it. Single-click the tray icon to reopen it; right-click for **Open Harmony Prompts**, **Settings…**, or **Exit**. A second launch reports that the existing tray instance is running.
- Settings explains storage and paste restrictions. Text size can be adjusted from 12 to 22; system colors and per-monitor DPI awareness are used. DPI, screen-reader and multiple-monitor acceptance still need manual testing.

### Local keyboard shortcuts

| Shortcut | Action |
|---|---|
| Ctrl+Shift+C | Copy the entire generated prompt |
| Ctrl+Alt+V | Request guarded Auto Paste |
| Ctrl+O | Import JSON |
| F5 | Reload from disk |
| Tab / Shift+Tab | Move through native controls |
| Ctrl+C in a text box | Normal selected-text copy (not intercepted) |

These are window-local, not global hotkeys. No continuous keyboard hook is installed. For shortcut-triggered Auto Paste, release modifiers; held Ctrl/Alt/Shift/Windows keys cause a bounded wait and then a manual-paste fallback.

## Auto Paste safety and limits

**The real-application safety acceptance is not complete.** Auto Paste stays OFF by default; no saved preference enables it on the next launch. The experimental session-only setting is not suitable for sensitive text. A pre-gate desktop test observed a different startup target than intended; it is recorded as a failure, not a pass, in [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md). Default operation is Copy + a manually verified Ctrl+V.

When explicitly enabled, Copy always happens first. The Windows adapter tracks foreground and window-destroy events, freezes a target HWND/PID/process-start identity and observed window-lifetime generation, rejects our own process, unsupported modal/shell targets and higher-integrity/session targets, and checks the exact actual foreground window before one four-event SendInput call. Arbitrary unsupported foreground windows clear an old candidate rather than silently reusing it. Recognized tray shell transitions may preserve the last candidate.

A guard rejects observed third-window interruptions. Foreground acquisition is bounded and never uses AttachThreadInput, input permissions, process elevation or foreground-lock changes to bypass OS rules. No prompt text is executed as a command. Clipboard operations use STA dispatch, bounded retries, a private per-copy marker plus payload comparison under an open clipboard transaction, and a sequence check before sending. Embedded NULs are rejected because CF_UNICODETEXT cannot faithfully represent them.

**Important OS limitation:** foreground checks, clipboard checks and global SendInput are not one atomic, HWND-addressed operation. Another window can take focus after the last check, including before queued input is consumed. HWND-destroy callbacks are also asynchronous. These protections reduce risk but **cannot prove that input will never reach the wrong window**. For sensitive text, use **Copy + a manually verified Ctrl+V**. Do not switch windows during Auto Paste. Until the full real-application acceptance matrix is completed, Auto Paste is not signed off as universally safe/compatible.

| Result | Meaning / next step |
|---|---|
| `CopiedOnly` | Clipboard write verified; paste manually. |
| `InputSent` | All four keyboard events were accepted, **not** proof the editor received text. Inspect the input. |
| `InputUncertain` | Partial insertion may already have caused a paste. Inspect the input **before retrying**, to avoid duplicates. No automatic retry occurs. |
| `KeyReleaseUncertain` | Windows did not confirm cleanup after partial input. Ctrl or V may remain down; press and release them to reset their state before continuing, then inspect the target before retrying. |
| `NoTarget` / `TargetGone` | Clipboard kept; select your input and paste manually. |
| `FocusDenied` | Focus was denied, unstable or interrupted; no full paste is attempted. |
| `InputBlocked` | Integrity/modifiers/clipboard change or native input blocked the attempt. Copy again if needed, then paste manually. |
| `ClipboardError` | Clipboard busy/unavailable or text cannot be represented; no paste is attempted. |

Never run Harmony as administrator to paste into an elevated app. Use manual paste there. Protected desktops, other Windows sessions, virtual desktops, unusual keyboard layouts/IMEs and terminal-specific paste confirmation policies can prevent automatic input. A target window being focused does not prove that its text editor is focused. Windows notifications may be suppressed by Do Not Disturb; reopen Harmony to see the status message.

### Clipboard privacy

The app does not read user clipboard content before copying, keep clipboard backups, clear the clipboard after failure, log prompts or upload anything. Each explicit copy includes raw DWORD-0 `CanIncludeInClipboardHistory` and `CanUploadToCloudClipboard` formats to request OS history/cloud opt-out. OS policies and third-party clipboard managers/readers are outside the app's control; no absolute clipboard privacy guarantee is possible. Copy replaces the current clipboard, as expected. Development/package restore downloads occur at build time, not in the running app.

## Data and JSON compatibility

- User data: `%APPDATA%\HarmonyPrompts\templates.json`.
- Bundle defaults: `Resources/templates.json`, copied from `shared/templates.json` at build/publish. Existing user files are **never** reseeded or overwritten by an upgrade/startup.
- Canonical defaults remain `HarmonyPrompts/HarmonyPrompts/Resources/templates.json` for macOS. A local test checks byte parity with `shared/templates.json`; no Xcode resource reference was changed.
- Top level remains a JSON **array**. See [templates.schema.json](../shared/templates.schema.json) and [fixture notes](../shared/test-fixtures/README.md).
- Required template properties: `id`, `name`, `description`, `template`, `fields`. Required field properties: `name`, `label`, `type`. Optional field properties retain explicit null/empty distinctions. Unknown extension properties are preserved when importing/serializing.
- Types are exactly lowercase `text`, `textarea`, `select`, `radio`, `checkbox`. Checkbox defaults/custom values and dropdown option order follow the Swift app. Duplicate field names and duplicate template IDs are explicitly rejected instead of silently discarding data or triggering Swift's duplicate-key trap.
- Placeholder matching is case-sensitive ASCII `[a-zA-Z0-9_-]+`; rendering is single-pass, non-recursive. Unknown placeholders stay visible.
- Imports must be nonempty; an already-existing valid `[]` file may be loaded and displays an empty state. Invalid JSON does not get deleted/reseeded.
- Imports are latest-request-wins. A later validated save supersedes older imports still reading/preparing. FIFO snapshot writes prepare a temporary file in the **same directory**, flush, atomically replace/move, and only then adopt memory. Failed writes clean temporary files and preserve the committed data. There is no undocumented `.bak` recovery scheme or automatic destructive repair.
- Back up your user JSON before intentionally replacing the collection. Form input is transient; template changes/defaults live in JSON, not automatically in a settings file.
- `.NET` and Swift may use different Unicode versions; fixtures cover selected emoji/combining/whitespace cases, not every future Unicode sequence. A Mac runtime comparison has not been run here.

## Build and debug locally

Install the **.NET 10 SDK** on Windows (or Visual Studio with the compatible .NET desktop workload). `Windows/global.json` specifies SDK `10.0.100` with `latestFeature` roll-forward within .NET 10; verification used `10.0.402`. Standard build and RID publish use separate NuGet lock files so a publish does not invalidate the next locked standard restore. Production code uses only framework libraries; no third-party runtime NuGet packages. Tests use locked xUnit/test SDK packages. SDK selection searches from the current directory, so `Windows/global.json` applies when running inside `Windows`; the root-directory commands below assume the .NET 10 SDK is selected on PATH.

From the repository root in PowerShell:

```powershell
dotnet --info
dotnet restore Windows/HarmonyPrompts.slnx --locked-mode
dotnet build Windows/HarmonyPrompts.slnx -c Release --no-restore
dotnet test Windows/HarmonyPrompts.Tests/HarmonyPrompts.Tests.csproj -c Release --no-build --no-restore
dotnet run --project Windows/HarmonyPrompts.Windows -c Release --no-build
```

For Debug, use `dotnet build Windows/HarmonyPrompts.slnx -c Debug` and `dotnet run --project Windows/HarmonyPrompts.Windows -c Debug --no-build`, or open `Windows/HarmonyPrompts.slnx` in a compatible Visual Studio. The Core and linked ViewModel tests run on `net10.0` without WPF; building/running WPF and the desktop harness requires Windows.

This development host had no SDK initially; verification used an official user-local SDK at `%LOCALAPPDATA%\HarmonyBuild\dotnet\dotnet.exe`. That path is not a product dependency. With a user-local SDK, set `DOTNET_ROOT` to its folder when running framework-dependent `.exe` files; the portable ZIP does not need this.

### Controlled desktop harness

`HarmonyPrompts.PasteHarness` is a **local interactive test utility**, not part of the shipped app. It opens synthetic WPF targets in separate processes. It does not read user documents or arbitrary clipboard contents into a log. It temporarily replaces the clipboard with synthetic data; save anything you need first. Let the test windows retain focus until completion; OS focus denial is a legitimate test failure, not something the harness bypasses.

```powershell
# Interactive target: paste manually into this synthetic input.
dotnet run --project Windows/HarmonyPrompts.PasteHarness -c Release --no-build

# Native clipboard / foreground / cross-process SendInput checks + product UI.
& .\Windows\HarmonyPrompts.PasteHarness\bin\Release\net10.0-windows\HarmonyPrompts.PasteHarness.exe `
  --verify "$PWD\Windows\HarmonyPrompts.Windows\bin\Release\net10.0-windows\HarmonyPrompts.exe"

# Product-only UI checks; verifies Auto Paste stays OFF, sends no global input.
& .\Windows\HarmonyPrompts.PasteHarness\bin\Release\net10.0-windows\HarmonyPrompts.PasteHarness.exe `
  --verify-app "$PWD\Windows\artifacts\publish\win-x64\HarmonyPrompts.exe"
```

Controlled native checks cover Unicode payload/marker, raw history/cloud flags, failed OLE flush/cancellation stream lifetime, stale sequence, NUL rejection, actual cross-process paste, third-process focus interruption, clipboard replacement during activation, unsupported external modal target, busy clipboard, and target destruction. Product checks cover startup/default seed, accessible dynamic text field/preview, filtering/restored selection, unchanged reload, Copy, Auto Paste disabled by default, and close-to-tray process survival. They never enable experimental product Auto Paste: launch-time focus can change before the app captures a target. Direct native checks instead freeze and validate their own synthetic HWND/PID. Product Auto Paste and its real-app acceptance remain manual/open.

Product UI checks **skip** if an existing user `templates.json` or app instance is found. They never edit an existing collection; the run's own unchanged default seed is removed on cleanup. Only owned child processes are terminated afterward. Shell tray reopen/menu Exit/disposal, five-control manual interactions, editors/browsers/Terminal, elevated windows and DPI are separate manual acceptance items. Controlled-target success is not evidence those applications passed. Use [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) to record each real result.

### Portable packaging

```powershell
dotnet publish Windows/HarmonyPrompts.Windows/HarmonyPrompts.Windows.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
  -o Windows/artifacts/publish/win-x64
Copy-Item Windows/README.md Windows/artifacts/publish/win-x64/README.md
Copy-Item Windows/IMPLEMENTATION_STATUS.md Windows/artifacts/publish/win-x64/IMPLEMENTATION_STATUS.md
Copy-Item LICENSE Windows/artifacts/publish/win-x64/LICENSE
$dotnetHome = Split-Path (Get-Command dotnet).Source
Copy-Item "$dotnetHome/LICENSE.txt" Windows/artifacts/publish/win-x64/DOTNET_LICENSE.txt
Copy-Item "$dotnetHome/ThirdPartyNotices.txt" Windows/artifacts/publish/win-x64/DOTNET_ThirdPartyNotices.txt
Compress-Archive -Path Windows/artifacts/publish/win-x64/* `
  -DestinationPath Windows/artifacts/HarmonyPrompts-1.0.0-win-x64.zip -Force
$hash = Get-FileHash Windows/artifacts/HarmonyPrompts-1.0.0-win-x64.zip -Algorithm SHA256
$hash
"$($hash.Hash.ToLowerInvariant())  HarmonyPrompts-1.0.0-win-x64.zip" | `
  Set-Content Windows/artifacts/HarmonyPrompts-1.0.0-win-x64.zip.sha256 -Encoding ascii
```

Single-file native dependencies are extracted by .NET into its per-user bundle cache on first launch. The bundle still intentionally requires the adjacent `Resources/templates.json` for seeding. Build/obj/artifacts/test outputs are ignored. No installer, code-signing, hosted release/upload, commit/push or CI/CD configuration is performed by this work; CI/CD changes are prohibited by the local project rules.

## Troubleshooting

- **Nothing visible after close:** look in the tray overflow area. Single-click or Open from the tray menu. Exit there before rebuilding/replacing files.
- **Already running:** use the existing tray instance. The app deliberately does not launch duplicate instances.
- **No target / wrong target label:** click your intended input, then reopen Harmony. Use manual copy/paste if in doubt; the app never searches arbitrary editors for a destination.
- **Copy & Paste disabled:** intentional default while safety acceptance remains open. Use Copy + verified manual Ctrl+V. Settings allows an experimental, session-only opt-in for non-sensitive test text; do not enable it expecting guaranteed destination/delivery.
- **Focus denied / admin target:** copy manually. Do not disable protections or elevate Harmony.
- **Clipboard busy:** close/release the app holding the clipboard and retry Copy. The retry loop is bounded and no input is sent on copy failure.
- **Corrupt user JSON / failed import:** the status message reports validation/access failure; original data is preserved. Open Folder, back up and repair the file or import a valid collection. Imports replace all templates.
- **Missing defaults / startup failure:** extract the entire ZIP with `Resources/templates.json`. Do not run only the `.exe` copied out of its directory.
- **No notification:** check the in-window status message; Windows may suppress notification balloons.
- **Unicode count differs on a Mac:** retain a minimal synthetic fixture with runtime/Unicode versions; do not log a private prompt as a diagnostic.
