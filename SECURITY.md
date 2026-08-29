# Security Policy

Harmony Prompts takes security and user privacy seriously. As a macOS native productivity tool designed to interact with developer workflows and AI coding agents, our top priority is ensuring that your prompts, local configuration, and clipboard operations remain completely secure and private.

---

## Supported Versions

Only the latest stable release of Harmony Prompts receives security updates and vulnerability patches.

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

---

## Reporting a Vulnerability

If you discover a security vulnerability or security concern within Harmony Prompts, please report it responsibly:

1. **Do not create a public GitHub issue** to report sensitive security vulnerabilities.
2. **Submit a Private Vulnerability Report**:
   - Use [GitHub Security Advisories](https://github.com/Poseidoncode/HarmonyApp/security/advisories/new) if enabled on the repository.
   - Or contact the maintainers directly via email at: `posidomhu@gmail.com` with the subject tag `[SECURITY: HarmonyApp]`.
3. **Please include in your report**:
   - A detailed description of the vulnerability and its potential impact.
   - Exact steps to reproduce or a minimal Proof of Concept (PoC).
   - The version of macOS and Harmony Prompts you are running.
   - Any relevant logs or screenshots.

### Response Timeline
- **Acknowledgment**: Within 48 hours of report receipt.
- **Assessment & Triage**: Within 5 business days.
- **Fix & Disclosure**: Maintainers will collaborate with the reporter to verify and deploy a fix before any public announcement.

---

## Security Architecture & Threat Model

Harmony Prompts follows a **Zero Data Leakage / Local-First** design:

### 1. 100% Local & Privacy-Preserving
- **No Outbound Network Connections**: Harmony Prompts does not transmit any user prompts, clipboard contents, or analytics to remote servers. All prompt rendering, template parsing, and storage take place strictly on your local machine.
- **No Telemetry / No Tracking**: No analytics libraries or tracking SDKs are included.

### 2. Apple Silicon & macOS Hardened Runtime
- **Hardened Runtime Enabled**: The application is compiled with Apple's Hardened Runtime (`ENABLE_HARDENED_RUNTIME = YES`) to mitigate memory corruption and binary tampering risks.
- **Modern Security Baselines**: Built targeting macOS 14.0+, leveraging Swift type-safety and ARC memory management without raw pointer misuse.

### 3. Accessibility & System Event Automation
- **Purpose**: Accessibility permission is requested solely to detect the frontmost active application (e.g., Cursor, Xcode, Terminal, Browser) and simulate `⌘V` (paste) upon user triggering.
- **Input Sanitization**:
  - Simulated key events are dispatched via native `CGEvent`.
  - Secondary automation scripts (AppleScript) utilize strict character escaping for process identifiers and names to prevent AppleScript injection attacks.
- **No Keystroke Logging**: The app does not intercept, log, or record global user keystrokes. Mouse click monitoring is only used to refresh the active frontmost window reference.

### 4. File Storage & Configuration Integrity
- **Sandboxed Storage**: Custom prompt templates are stored in `~/Library/Application Support/HarmonyPrompts/templates.json`.
- **Atomic Operations**: All save actions use atomic file writing (`.atomic`) to prevent data corruption during unexpected shutdowns.
- **Strict Parsing**: Imported template files are strictly validated against strongly-typed Swift models (`Decodable`). Untrusted keys or arbitrary executable scripts in JSON files are discarded.

---

## Security Best Practices for Users

- **Prompt Content**: Never store hardcoded production credentials, private keys, or API tokens in shared or version-controlled `templates.json` files.
- **External Imports**: Inspect JSON template files before importing them into Harmony Prompts if obtained from untrusted external sources.
- **Installation Verification**: Always verify repository integrity when installing via `install.sh` or build directly from the official repository source.
