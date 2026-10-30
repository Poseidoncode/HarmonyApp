# Windows Implementation Status

**Date:** 2026-10-23  
**Overall:** Windows source implementation and local build/test/publish are complete. **The full PLAN.md Definition of Done is NOT complete.** Auto Paste safety acceptance, external-app/environment validation and macOS runtime validation remain open. Auto Paste is **OFF by default**, with an explicit experimental opt-in for the current session only. Normal use is Copy + a manually verified Ctrl+V.

- Base commit: `f024a0d7c8608f20b14689ccf7bc93da19a29632` (HEAD unchanged; no commit/push).
- Baseline worktree: only the user's untracked `PLAN.md`; no Windows code existed.
- Host: Windows x64 `10.0.26300`, interactive desktop, Git Bash/PowerShell, no Xcode/Visual Studio verification.
- SDK: official user-local .NET SDK `10.0.402`; runtime `10.0.13`, at `%LOCALAPPDATA%\HarmonyBuild\dotnet` (not a product dependency).
- SDK policy: `Windows/global.json` minimum `10.0.100`, `latestFeature` roll-forward within .NET 10; only `10.0.402` was actually tested.
- **CI/CD excluded:** local agent/project rules prohibit adding/modifying CI/CD. No workflow or other CI/CD configuration was created. No hosted build/release, upload, installer, signing, commit or push is claimed.

## Phase tracking

| Phase | Implementation | Actual acceptance |
|---|---|---|
| 0 — baseline/schema/fixtures | Complete | Local shared/Mac JSON byte parity PASS; Swift source semantics examined, no Mac runtime comparison |
| 1 — tray/focus/clipboard/paste | Complete, experimental Auto Paste gated OFF | Nine named controlled native desktop checks PASS; real-app safety acceptance NOT complete; tray menu/reopen/Exit manual checks open |
| 2 — Core | Complete | Compatibility/roundtrip/Unicode/default/renderer tests PASS |
| 3 — repository | Complete | Real temporary-file tests and injected concurrency/failure tests PASS |
| 4 — MVVM/WPF UI | Complete | ViewModel tests and three named product desktop checks PASS; five-control manual/DPI/multi-monitor/screen-reader checks open |
| 5 — docs/local publish | Complete for local ZIP route | Release build, unit tests, self-contained publish and local published-product smoke PASS; clean VMs/external apps/Mac/signing/hosted release NOT run; CI/CD prohibited |

`PLAN.md` checkboxes track implementation, not an assertion that every manual acceptance condition passed.

## Delivered changes

- `Windows/HarmonyPrompts.slnx`: Core, native WPF app, xUnit tests and a local interactive desktop harness.
- `Windows/HarmonyPrompts.Core/Models` and `Windows/HarmonyPrompts.Core/Services`: exact lowercase JSON field types, optional null/empty/unknown-property roundtrip, field defaults, single-pass rendering, Unicode grapheme/Swift whitespace counts and guarded paste state machine.
- `Windows/HarmonyPrompts.Core/Storage`: WPF-independent transactional repository. Same-directory prepared writes, flush/atomic replace/move, FIFO saves, latest mutation/import tickets, no reseed of existing files, commit-before-memory adoption and fault cleanup. This is intentionally in Core rather than the plan's suggested Windows services folder so file/concurrency logic can be tested without WPF.
- `Windows/HarmonyPrompts.Windows`: native XAML/MVVM, five generated control types, search/selection restoration, live preview/counts, explicit JSON replacement import/reload, user-folder access, settings, local shortcuts/text scale, icon/version/manifest, notification icon and single instance.
- Native adapters: STA clipboard, bounded contention retries, raw Unicode/operation-marker/privacy-format verification in one open transaction; foreground/destroy hooks, HWND/PID/start-time/observed-lifetime checks, exact focus/UIPI/session/desktop/modifier gates, bounded activation and partial key-release handling.
- `Windows/HarmonyPrompts.Tests`: **125 passing tests**, including the exact production ViewModels and native event-count policy source-linked into a WPF-free test assembly.
- `Windows/HarmonyPrompts.PasteHarness`: owned synthetic WPF target/decoy processes plus UIAutomation smoke. It does not enable experimental product Auto Paste. Existing user template files/app instances cause a safe product-test SKIP; only the run's own unchanged seed is removed.
- `shared/templates.json`, `shared/templates.schema.json`, `shared/test-fixtures`: defaults copied from the Mac resource, documented golden inputs/outputs and local parity test.
- Root `README.md`, `Windows/README.md` and this report: build/run/package/use/troubleshooting, data/import behavior and explicitly incomplete acceptance.
- Local portable output: `Windows/artifacts/HarmonyPrompts-1.0.0-win-x64.zip` (ignored; not uploaded or committed), containing self-contained executable, `Resources/templates.json`, README and license. PDBs in the publish directory are optional diagnostic metadata, not prompt logs.

## Decisions and safety boundaries

- Keep **all** existing macOS Swift, Xcode resources/project, scripts and install flow unchanged. The Mac JSON remains canonical; Windows build/publish copies `shared/templates.json`. A test compares both files byte-for-byte.
- No third-party **runtime** packages, accounts, telemetry, application network requests, auto-update, startup registration or continuous keyboard hook. Test packages use explicit versions/lock files. NuGet/build-time downloads are not running-app network calls.
- Windows user data is `%APPDATA%\HarmonyPrompts\templates.json`. Imports replace, not merge. Empty imports, malformed JSON and failures preserve committed data; a valid pre-existing `[]` loads as an empty collection. No undocumented destructive repair/backup scheme.
- Duplicate field names are explicitly rejected (Swift's `Dictionary(uniqueKeysWithValues:)` traps); duplicate template IDs also produce a clear validation error. Optional null versus empty values and unknown JSON extension data are preserved.
- Unicode counts are grapheme-based with Swift-compatible whitespace fixtures, not UTF-16 length. Future .NET/Swift Unicode-version segmentation differences remain possible; no universal Unicode/Mac-runtime claim.
- **No absolute no-wrong-window guarantee:** Win32 offers no atomic target-bound clipboard/focus/SendInput operation. Even a correct final foreground check can race focus changes and queued-input consumption. Destroy hooks are asynchronous; the observed lifetime generation is not a kernel HWND identity certificate.
- **Auto Paste default OFF:** session-only explicit Settings opt-in, never persisted. Disabled commands cannot write the clipboard or send input; Copy remains available. Experimental mode is not suitable for sensitive text. This follows PLAN.md's safety gate rather than calling unfinished acceptance complete.
- SendInput success means **input accepted**, never “text delivered.” Partial insertion is `InputUncertain`; incomplete cleanup is `KeyReleaseUncertain` with an explicit Ctrl/V reset warning. No second V-down or automatic paste retry is sent.
- OS clipboard history/cloud opt-out formats are raw DWORD zero and verified with the payload/marker before certifying a copy. Managed streams remain owned by the DataObject after failed OLE flush/cancellation. OS policies/third-party clipboard readers remain outside app control; there is no absolute clipboard privacy guarantee.
- Portable ZIP chosen over installer: no elevation, registry/startup changes or untested installer upgrade/uninstall machinery. App-folder replacement leaves `%APPDATA%` intact. No signing certificate was available, so the build is unsigned; never disable OS protections to run it.
- Standard and explicit-RID publish NuGet locks are separate (`packages.lock.json` / `packages.win-x64.lock.json`). A publish originally invalidated normal locked restore (`NU1004`); RID-specific lock paths fixed this, and the publish → normal locked-restore round trip was actually rerun successfully.

## Verification — commands and actual results

From repository root in PowerShell, using an installed SDK on PATH. This host used `%LOCALAPPDATA%\HarmonyBuild\dotnet\dotnet.exe` instead.

```powershell
dotnet restore Windows/HarmonyPrompts.slnx --locked-mode
dotnet build Windows/HarmonyPrompts.slnx -c Release --no-restore
dotnet test Windows/HarmonyPrompts.Tests/HarmonyPrompts.Tests.csproj -c Release --no-build --no-restore
```

- **PASS:** locked restore.
- **PASS:** Release **and Debug** builds of all four projects, each **0 warnings / 0 errors**, warnings treated as errors. Debug was additionally checked with `dotnet build Windows/HarmonyPrompts.slnx -c Debug --no-restore`.
- **PASS:** latest full xUnit run: **125 passed / 0 failed / 0 skipped**. Tests include renderer/fixtures/defaults/schema-contract roundtrip, real file operations, denied access/locked files, corrupt/empty JSON, cancellation/temp cleanup, FIFO snapshots, slow imports invalidated by newer imports or validated saves, unchanged reload/filter form retention, all five field ViewModels, paste guard state transitions, 0–4 SendInput event counts and incomplete cleanup, explicit default-off/session opt-in and key-release warnings.
- Targeted follow-up review ran **45** then-current paste/ViewModel tests successfully; full main run above supersedes that partial count. Read-only data/UI and final clipboard/key-release/lock reviews reported no remaining actionable findings in their targeted scope. Review is not proof of universal Windows safety.

### Controlled native desktop checks

```powershell
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\HarmonyBuild\dotnet" # only for this user-local SDK host
& .\Windows\HarmonyPrompts.PasteHarness\bin\Release\net10.0-windows\HarmonyPrompts.PasteHarness.exe --verify
```

**Latest isolated native run: PASS, exit 0, all nine named groups:**

1. Unicode payload + per-copy marker/sequence + raw history/cloud exclusion formats.
2. Competing clipboard writer invalidates certificate; embedded NUL is rejected.
3. OLE publication succeeds but flush fails/cancels: no copy certification; privacy streams remain renderable.
4. A separate owned WPF process receives Ctrl+V and the exact synthetic Unicode/CRLF text.
5. A real owned third-process foreground interruption sends no text to target or decoy.
6. Clipboard replacement during activation blocks input.
7. An unsupported external modal dialog clears the old target candidate.
8. An owned process holding the real clipboard causes a bounded copy error, then releases it.
9. Native target destruction invalidates identity and leaves copied text for manual fallback.

This is real Windows clipboard/foreground/SendInput execution, not mocks, but **only synthetic controlled WPF targets**, not VS Code/Cursor/browser/Terminal acceptance.

### Published product desktop smoke

```powershell
& .\Windows\HarmonyPrompts.PasteHarness\bin\Release\net10.0-windows\HarmonyPrompts.PasteHarness.exe `
  --verify-app "$PWD\Windows\artifacts\publish\win-x64\HarmonyPrompts.exe"
```

**PASS, exit 0, three named groups:** actual self-contained product startup/seed + accessible text field/live preview + search/visible-selection restoration + unchanged reload + clipboard Copy; experimental Auto Paste disabled by default/no input sent; close leaves the process alive for tray. No existing user collection was used. The final ZIP was also extracted, executable/resource/docs/license hashes compared with publish output, and the extracted product passed these same three checks. This is **not** a fresh-VM test.

### Failed/intermittent runs retained as findings

Earlier interactive runs were **not all green**: `timeout: driver foreground`, `timeout: actual cross-process Unicode text arrival`, a clipboard-state/foreground interruption assertion, target-capture instability and transient inability to read clipboard privacy formats occurred. The foreground partner callback and bounded clipboard-format-read wait made controlled testing repeatable enough for the successful isolated run; no OS foreground restrictions were bypassed. These failures mean the harness requires an uninterrupted interactive desktop and do not establish real-app reliability.

Most importantly, a pre-default-off product smoke failed `timeout: product Copy & Paste reaches controlled target`: controlled output length was **0 vs expected 116**, while product status reported `InputSent` to an unrelated process (`Zed`), not the owned test window. Arrival elsewhere was **not inspected or confirmed**, and no unrelated document was read or automatically repaired. This is an explicit failed safety/intent observation, not a successful Zed test. Product Auto Paste was subsequently gated OFF by default, and final automated product smoke never enables it. Startup target capture and global-input races remain acceptance blockers in experimental mode.

### Self-contained publish / packaging

```powershell
dotnet publish Windows/HarmonyPrompts.Windows/HarmonyPrompts.Windows.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
  -o Windows/artifacts/publish/win-x64
dotnet restore Windows/HarmonyPrompts.slnx --locked-mode
```

- **PASS:** self-contained compressed single-file publish with adjacent `Resources/templates.json`; normal locked restore also passes afterward.
- **PASS:** local ZIP creation, extraction/content hash checks and extracted-product smoke (all three named product groups, exit 0). A detached SHA-256 checksum is generated with the final archive as `Windows/artifacts/HarmonyPrompts-1.0.0-win-x64.zip.sha256`. Both the project MIT license and .NET license/third-party notices are included.
- **NOT RUN:** clean Windows 10/11 VM, installer install/upgrade/uninstall, signing/SmartScreen acceptance, hosted release.
- macOS integrity check: `git diff --quiet HEAD -- HarmonyPrompts install.sh` **PASS**; local JSON parity test **PASS**. Xcode build/Swift fixture runtime comparison **NOT RUN**.

## PLAN.md end-to-end matrix

`CONTROLLED PASS` is deliberately not a manual real-app/environment acceptance pass.

| ID | Actual result / remaining scope |
|---|---|
| CORE-01 | PASS — known placeholder replacement |
| CORE-02 | PASS — unknown placeholder retained |
| CORE-03 | PASS — nonrecursive single pass |
| CORE-04 | PASS — ASCII keys/case/underscore/hyphen |
| CORE-05 | PASS — five field types/default/null/empty/custom checkbox cases |
| CORE-06 | PASS — selected Unicode/whitespace fixtures; Mac runtime NOT RUN |
| JSON-01 | PASS — defaults roundtrip, optional presence/extension data, Mac/shared bytes |
| JSON-02 | PASS — invalid/empty import leaves existing memory/disk |
| JSON-03 | PASS — newest-ticket semantics, older prepared/read imports superseded |
| JSON-04 | PASS — serialized save/reload and failure/cancellation tests |
| UI-01 | CONTROLLED PASS — text field/search/preview; five-control/manual workflow NOT RUN |
| UI-02 | CONTROLLED PASS — unchanged reload retains typed form |
| PASTE-01 | NOT RUN — VS Code |
| PASTE-02 | NOT RUN — Cursor |
| PASTE-03 | NOT RUN — Chrome/Edge input |
| PASTE-04 | NOT RUN — Windows Terminal |
| PASTE-05 | CONTROLLED PASS — owned native target destruction; external-app matrix open |
| PASTE-06 | CONTROLLED PASS — observed owned third-window interruption; absolute destination safety NOT proven; pre-gate product target-intent failure recorded above |
| PASTE-07 | Unit denial checks PASS; actual elevated-target test NOT RUN, no elevation attempted |
| PASTE-08 | CONTROLLED PASS — real clipboard contention; bounded failure/recovery |
| PASTE-09 | NOT RUN — other IMEs/layouts |
| PACK-01 | Local self-contained publish/extraction/content hashes/product smoke PASS; clean VM NOT RUN |
| PACK-02 | Close-to-tray process survival CONTROLLED PASS; shell tray reopen/menu Exit/disposal manual check NOT RUN |
| MAC-01 | Source/resource/install-flow unchanged; Xcode build NOT RUN |

## Remaining acceptance / next actions

- Keep Copy as the recommended mode and Auto Paste default OFF. Before considering enabling it by default, resolve/validate startup target-intent behavior and perform repeated manual wrong-window/interruption tests; global SendInput cannot become an atomic HWND-targeted operation.
- Complete VS Code/Cursor/Chrome/Edge/Terminal and elevated-target/virtual-desktop/IME/layout matrix with synthetic non-sensitive text. Record actual results individually; no application names above are implicit passes.
- Exercise all five real controls, import picker (replace/invalid/cancel), folder access, settings, screen-reader/Tab flow, 125%/150% DPI and multiple monitors.
- Verify tray shell reopen, single-instance user experience, Settings/Exit and hook/icon disposal manually.
- Test clean supported/serviced Windows 10 and Windows 11 x64 VMs/standard accounts and unsigned portable upgrade/removal behavior preserving user data. Windows 10 edition/support status matters for .NET 10.
- Run original Xcode build and shared golden fixtures on a Mac; retain source/hash parity meanwhile.
- No CI/CD work is proposed or performed under the local policy; it is an explicitly unmet portion of the original plan.
